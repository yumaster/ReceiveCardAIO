using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ArcFaceSharp.Model
{
    public class MultiFaceModel : IDisposable
    {
        /// <summary>
        /// 多人脸信息
        /// </summary>
        public ASF_MultiFaceInfo MultiFaceInfo { get; private set; }

        /// <summary>
        /// 单人脸信息List
        /// </summary>
        public List<ASF_SingleFaceInfo> FaceInfoList { get; private set; }

        public MultiFaceModel() { }

        public MultiFaceModel(ASF_MultiFaceInfo multiFaceInfo) 
        {
            this.MultiFaceInfo = multiFaceInfo;
            this.FaceInfoList = new List<ASF_SingleFaceInfo>();
            FaceInfoList = PtrToMultiFaceArray(multiFaceInfo.faceRects, multiFaceInfo.faceOrients, multiFaceInfo.faceNum);
        }

        private List<ASF_SingleFaceInfo> PtrToMultiFaceArray(IntPtr faceRect, IntPtr faceOrient, int length)
        {
            List<ASF_SingleFaceInfo> FaceInfoList = new List<ASF_SingleFaceInfo>();
            var size = Marshal.SizeOf(typeof(int));
            var sizer = Marshal.SizeOf(typeof(MRECT));

            for (var i = 0; i < length; i++)
            {
                ASF_SingleFaceInfo faceInfo = new ASF_SingleFaceInfo();

                MRECT rect = new MRECT();
                var iPtr = new IntPtr(faceRect.ToInt32() + i * sizer);
                rect = (MRECT)Marshal.PtrToStructure(iPtr, typeof(MRECT));
                faceInfo.faceRect = rect;

                int orient = 0;
                iPtr = new IntPtr(faceOrient.ToInt32() + i * size);
                orient = (int)Marshal.PtrToStructure(iPtr, typeof(int));
                faceInfo.faceOrient = orient;
                FaceInfoList.Add(faceInfo);
            }
            return FaceInfoList;
        }

        public void Dispose()
        {
            Marshal.FreeCoTaskMem(MultiFaceInfo.faceRects);
            Marshal.FreeCoTaskMem(MultiFaceInfo.faceOrients);
        }
    }
}
