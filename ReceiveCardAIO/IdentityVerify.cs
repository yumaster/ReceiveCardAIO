using AForge.Video.DirectShow;
using ArcFaceSharp.Model;
using ArcFaceSharp.SDKAPI;
using ArcFaceSharp.Util;
using IDCard;
using ReceiveCardAIO.Common;
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ReceiveCardAIO
{
    // 第一步：声明一个委托。（根据自己的需求）
    public delegate void setResultValue(bool retValue);
    public partial class IdentityVerify : Form
    {
        private IntPtr pImageEngine = IntPtr.Zero;//==========图片引擎Handle
        private float threshold = 0.7f;//相似度  阈值设置为0.8
        private Image defaultImage;//默认显示的照片
        //==========视频引擎Handle
        private IntPtr pVideoEngine = IntPtr.Zero;//视频引擎Handle
        private IntPtr pVideoImageEngine = IntPtr.Zero;//视频引擎 FR Handle 处理   FR和图片引擎分开，减少强占引擎的问题
        private FilterInfoCollection filterInfoCollection; // 视频输入设备信息
        private VideoCaptureDevice deviceVideo;//摄像头每一帧的画面的信息。
        private FaceTrackUnit trackUnit = new FaceTrackUnit();//视频检测缓存实体类
        private Font font = new Font(FontFamily.GenericSerif, 10f);//定义特定的文本格式，包括字体、字号和样式特性。
        private SolidBrush brush = new SolidBrush(Color.Red);// 定义一种颜色的画笔。 画笔用于填充图形形状，如矩形、 椭圆、 饼、 多边形和路径。
        private Pen pen = new Pen(Color.Red);//定义用于绘制直线和曲线的对象。
        private bool isLock = false;//下面用于同步的锁变量
        /*
         * 人脸验证的相关参数
         */
        private int recTimes = 8;//人脸识别的次数,大于三次还没有匹配成功就要进行额外的验证
        private bool faceStop = false;//检测是否要停止人脸检测
        //是否通过闸机
        private int pass = 0; //0 没有通过 1 人脸通过

        //=====身份证相关
        public IDCardHelper idCardHelper;//身份证识别帮助类
        private string m_strPath;//可执行程序所在的目录
        private delegate void AppendText(string text);//在线程中向lbl_msg中添加信息的委托
        private void AddTextToMessBox(string text)
        {
            lbl_msg.Text = (string.Format(text));
        }
        public event setResultValue setFormResultValue; //第二步：声明一个委托类型的事件
        /// <summary>
        /// 激活并初始化引擎
        /// </summary>
        private void ActiveAndInitEngines()
        {
            //读取配置文件中的 APP_ID 和 SDKKEY
            AppSettingsReader reader = new AppSettingsReader();
            string appId = (string)reader.GetValue("APP_ID", typeof(string));
            string sdkKey = (string)reader.GetValue("SDKKEY", typeof(string));
            int retCode = 0;
            //激活引擎    
            try
            {
                retCode = ASFFunctions.ASFActivation(appId, sdkKey);
            }
            catch (Exception ex)
            {
                //异常处理
                return;
            }
            #region 图片引擎pImageEngine初始化
            //初始化引擎
            uint detectMode = DetectionMode.ASF_DETECT_MODE_IMAGE;
            //检测脸部的角度优先值
            int detectFaceOrientPriority = ASF_OrientPriority.ASF_OP_0_HIGHER_EXT;
            //人脸在图片中所占比例，如果需要调整检测人脸尺寸请修改此值，有效数值为2-32
            int detectFaceScaleVal = 16;
            //最大需要检测的人脸个数
            int detectFaceMaxNum = 5;
            //引擎初始化时需要初始化的检测功能组合
            int combinedMask = FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_AGE | FaceEngineMask.ASF_GENDER | FaceEngineMask.ASF_FACE3DANGLE;
            //初始化引擎，正常值为0，其他返回值请参考http://ai.arcsoft.com.cn/bbs/forum.php?mod=viewthread&tid=19&_dsign=dbad527e
            retCode = ASFFunctions.ASFInitEngine(detectMode, detectFaceOrientPriority, detectFaceScaleVal, detectFaceMaxNum, combinedMask, ref pImageEngine);
            if (retCode == 0)
            {
                lbl_msg.Text=("图片引擎初始化成功!\n");
            }
            else
            {
                lbl_msg.Text = (string.Format("图片引擎初始化失败!错误码为:{0}\n", retCode));
            }
            #endregion

            #region 初始化视频模式下人脸检测引擎
            uint detectModeVideo = DetectionMode.ASF_DETECT_MODE_VIDEO;
            int combinedMaskVideo = FaceEngineMask.ASF_FACE_DETECT | FaceEngineMask.ASF_FACERECOGNITION;
            retCode = ASFFunctions.ASFInitEngine(detectModeVideo, detectFaceOrientPriority, detectFaceScaleVal, detectFaceMaxNum, combinedMaskVideo, ref pVideoEngine);
            if (retCode == 0)
            {
                lbl_msg.Text=("视频引擎初始化成功!\n");
            }
            else
            {
                lbl_msg.Text = (string.Format("视频引擎初始化失败!错误码为:{0}\n", retCode));
            }
            #endregion

            #region 视频专用FR引擎
            detectFaceMaxNum = 1;
            combinedMask = FaceEngineMask.ASF_FACERECOGNITION | FaceEngineMask.ASF_FACE3DANGLE | FaceEngineMask.ASF_LIVENESS;
            retCode = ASFFunctions.ASFInitEngine(detectMode, detectFaceOrientPriority, detectFaceScaleVal, detectFaceMaxNum, combinedMask, ref pVideoImageEngine);
            Console.WriteLine("InitVideoEngine Result:" + retCode);

            if (retCode == 0)
            {
                lbl_msg.Text = ("视频专用FR引擎初始化成功!\n");
            }
            else
            {
                lbl_msg.Text = (string.Format("视频专业FR引擎初始化失败!错误码为:{0}\n", retCode));
            }
            // 摄像头初始化
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            lbl_msg.Text = (string.Format("摄像头初始化完成...\n"));
            #endregion
        }
        /// <summary>
        /// 开启摄像头
        /// </summary>
        private void StartVideo()
        {
            //必须保证有可用摄像头
            if (filterInfoCollection.Count == 0)
            {
                lbl_msg.Text = ("未检测到摄像头，请确保已安装摄像头或驱动!");
            }
            if (videoSource.IsRunning)
            {
                videoSource.SignalToStop(); //关闭摄像头
                videoSource.Hide();
            }
            else
            {
                videoSource.Show();
                if (filterInfoCollection.Count > 0)
                {
                    deviceVideo = new VideoCaptureDevice(filterInfoCollection[0].MonikerString);
                    deviceVideo.VideoResolution = deviceVideo.VideoCapabilities[0];
                    videoSource.VideoSource = deviceVideo;
                    videoSource.Start();
                }
            }
        }

        public IdentityVerify()
        {
            InitializeComponent();
            //存放可执行文件所在的目录
            m_strPath = Application.StartupPath;
            //让其他线程也可以使用UI主线程的控件
            Control.CheckForIllegalCrossThreadCalls = false;
            //从摄像头提取图像与身份证证件照进行比对的操作
            ActiveAndInitEngines();
            //设置默认照片
            string filename = m_strPath + @"\default.gif";
            defaultImage = Image.FromFile(filename);
            this.IDPbox.Image = defaultImage;
        }
        /// <summary>
        /// 视频流渲染事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void videoSource_Paint(object sender, PaintEventArgs e)
        {
            //能够读到身份证信息 和 视频信息   这个判断很重要
            if (videoSource.IsRunning && (idCardHelper.idInfo != null) && (idCardHelper.idInfo.isRight == true))
            {
                this.IDPbox.Image = idCardHelper.idInfo.image;
                this.lbl_idInfo.Text = idCardHelper.idInfo.name + "\r\n" + idCardHelper.idInfo.sex + "\r\n" + idCardHelper.idInfo.nation + "\r\n" +
                    idCardHelper.idInfo.address + "\r\n" + idCardHelper.idInfo.ID + "\r\n" + idCardHelper.idInfo.org + "\r\n" + idCardHelper.idInfo.vaildity;

                while ((!faceStop) && (recTimes >= 0))  //只检测8帧人脸
                {
                    //得到当前摄像头下的图片
                    Bitmap bitmap = videoSource.GetCurrentVideoFrame();
                    //传入比对函数中进行比对
                    CompareImgWithIDImg(bitmap, e);
                }
            }
            recTimes = 8;
            faceStop = false;
        }
        /// <summary>
        /// 比对函数，将每一帧抓拍的照片和身份证照片进行比对
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool CompareImgWithIDImg(Bitmap bitmap, PaintEventArgs e)
        {
            recTimes--;
            if (bitmap == null)
            {
                return false;
            }
            Graphics g = e.Graphics;
            float offsetX = videoSource.Width * 1f / bitmap.Width;
            float offsetY = videoSource.Height * 1f / bitmap.Height;
            //检测人脸，得到Rect框
            ASF_MultiFaceInfo multiFaceInfo = FaceUtil.DetectFace(pVideoEngine, bitmap);
            //得到最大人脸
            ASF_SingleFaceInfo maxFace = FaceUtil.GetMaxFace(multiFaceInfo);
            //得到Rect
            MRECT rect = maxFace.faceRect;
            float x = rect.left * offsetX;
            float width = rect.right * offsetX - x;
            float y = rect.top * offsetY;
            float height = rect.bottom * offsetY - y;
            //根据Rect进行画框
            g.DrawRectangle(pen, x, y, width, height);
            //将上一帧检测结果显示到页面上
            g.DrawString(trackUnit.message, font, brush, x, y + 5);
            //保证只检测一帧，防止页面卡顿以及出现其他内存被占用情况
            if (isLock == false)
            {
                isLock = true;
                //异步处理提取特征值和比对，不然页面会比较卡
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
                {
                    if (rect.left != 0 && rect.right != 0 && rect.top != 0 && rect.bottom != 0)
                    {
                        try
                        {
                            //提取人脸特征
                            IntPtr feature = FaceUtil.ExtractFeature(pVideoImageEngine, bitmap, maxFace);
                            float similarity = CompareTwoFeatures(feature, idCardHelper.idInfo.imageFeature);
                            this.similarity.Text = ("相似度为: " + similarity.ToString("P")); ; //显示在界面上
                            this.similarity.ForeColor = similarity > threshold ? Color.Green : Color.Red;
                            //得到比对结果
                            int result = (CompareTwoFeatures(feature, idCardHelper.idInfo.imageFeature) >= threshold) ? 1 : -1;
                            if (result > -1)
                            {
                                bool isLiveness = false;
                                ImageInfo imageInfo = ImageUtil.ReadBMP(bitmap); //调整图片数据
                                if (imageInfo == null)
                                    return;
                                int retCode_Liveness = -1;
                                //RGB活体检测
                                ASF_LivenessInfo liveInfo = FaceUtil.LivenessInfo_RGB(pVideoImageEngine, imageInfo, multiFaceInfo, out retCode_Liveness);
                                //判断检测结果
                                if (retCode_Liveness == 0 && liveInfo.num > 0)
                                {
                                    int isLive = MemoryUtil.PtrToStructure<int>(liveInfo.isLive);
                                    isLiveness = (isLive == 1) ? true : false;
                                }
                                if (isLiveness)//活体检测成功
                                {
                                    //存放当前人脸识别的相似度
                                    idCardHelper.idInfo.similarity = similarity;
                                    //记录下当前的摄像头的人脸抓拍照
                                    idCardHelper.idInfo.capImage = bitmap;
                                    //验证通过则不再是当前身份证，等待下一次身份证
                                    idCardHelper.idInfo.isRight = false;
                                    //在子线程中输出信息到messageBox
                                    AppendText p = new AppendText(AddTextToMessBox);
                                    lbl_msg.Invoke(p, "人脸验证成功，请取卡...\n");
                                    pass = 1;
                                    idCardHelper.idInfo.isPass = 1;
                                    //将比对结果放到显示消息中，用于最新显示
                                    trackUnit.message = string.Format("通过验证，相似度为{0}", similarity);
                                    FileHelper.DeleteFile(m_strPath);   //删除验证过的本地文件
                                    Thread.Sleep(1000);//延时1秒
                                    this.IDPbox.Image = defaultImage;//照片恢复默认照片
                                    trackUnit.message = "";//人脸识别框文字置空
                                    setFormResultValue(true);
                                }
                                else
                                {
                                    pass = 0;//标志未通过
                                    trackUnit.message = "未通过，系统识别为照片";
                                    AppendText p = new AppendText(AddTextToMessBox);
                                    lbl_msg.Invoke(p, "抱歉，您未通过人脸验证...\n");
                                    FileHelper.DeleteFile(m_strPath);//删除验证过的本地文件
                                }
                            }
                            else
                            {
                                pass = 0;//标志未通过
                                trackUnit.message = "未通过人脸验证";
                                AppendText p = new AppendText(AddTextToMessBox);
                                lbl_msg.Invoke(p, "抱歉，您未通过人脸验证...\n");
                                FileHelper.DeleteFile(m_strPath);//删除验证过的本地文件
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            FileHelper.DeleteFile(m_strPath);//删除验证过的本地文件
                        }
                        finally
                        {
                            isLock = false;
                        }
                    }
                    isLock = false;
                }));
            }
            return false;
        }

        /// <summary>
        /// 比较两个特征值的相似度,返回相似度
        /// </summary>
        /// <param name="feature1"></param>
        /// <param name="feature2"></param>
        /// <returns></returns>
        private float CompareTwoFeatures(IntPtr feature1, IntPtr feature2)
        {
            float similarity = 0.0f;
            //调用人脸匹配方法，进行匹配
            ASFFunctions.ASFFaceFeatureCompare(pVideoImageEngine, feature1, feature2, ref similarity);
            return similarity;
        }
        /// <summary>
        /// 窗体加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdentityVerify_Load(object sender, EventArgs e)
        {
            //身份证读卡子线程
            //为了将身份证的信息读到内存中来
            idCardHelper = new IDCardHelper();
            idCardHelper.GetAvatarInfo(pImageEngine, m_strPath);
            StartVideo();//开启视频
        }
        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdentityVerify_FormClosed(object sender, FormClosedEventArgs e)
        {
            //销毁引擎
            int retCode = ASFFunctions.ASFUninitEngine(pImageEngine);
            Console.WriteLine("UninitEngine pImageEngine Result:" + retCode);
            //销毁引擎
            retCode = ASFFunctions.ASFUninitEngine(pVideoEngine);
            Console.WriteLine("UninitEngine pVideoEngine Result:" + retCode);
            //销毁引擎
            retCode = ASFFunctions.ASFUninitEngine(pVideoImageEngine);
            Console.WriteLine("UninitEngine pVideoImageEngine Result:" + retCode);
            if (videoSource.IsRunning)
            {
                videoSource.SignalToStop(); //关闭摄像头
            }
            idCardHelper.CloseService();
            this.Dispose();
            this.Close();
            MemoryUtil.ClearMemory();
        }
    }
}
