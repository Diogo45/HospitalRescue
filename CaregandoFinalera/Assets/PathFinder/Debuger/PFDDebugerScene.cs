#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace K_PathFinder.PFDebuger {
    [ExecuteInEditMode()]
    public class PFDDebugerScene : MonoBehaviour {
        enum Indexes : int {
            ImportantDot = 0,
            ImportantLine = 1,
            ImportantTris = 2,
            Path = 3,
            GenericDot = 4,
            GenericLine = 5,
            GenericMesh = 6,
        }
        MaterialAndBufferHolder[] data;
        Action updateDelegate;

        //int maxTest = 1000;
        //int curTest = 0;

        //name of parameters in shader
        private const string dotShaderParameterName = "point_data";
        private const string lineShaderParameterName = "line_data";
        private const string trisShaderParameterName = "triangle_data";

        //size of struct
        private const int dotStride = (sizeof(float) * (3 + 4 + 1));
        private const int lineStride = (sizeof(float) * (3 + 3 + 4 + 1));
        private const int trisStride = (sizeof(float) * (3 + 3 + 3 + 4));        

        void OnEnable() {
            Debuger_K.ForceInit();
            Shader dotShader = PFDSettings.GetDotShader();
            Shader lineShader = PFDSettings.GetLineShader();
            Shader trisShader = PFDSettings.GetTrisShader();  

            data = new MaterialAndBufferHolder[7];
            data[(int)Indexes.ImportantDot] = new MaterialAndBufferHolder(dotShader, dotShaderParameterName, dotStride);
            data[(int)Indexes.ImportantLine] = new MaterialAndBufferHolder(lineShader, lineShaderParameterName, lineStride);
            data[(int)Indexes.ImportantTris] = new MaterialAndBufferHolder(trisShader, trisShaderParameterName, trisStride);
            data[(int)Indexes.Path] = new MaterialAndBufferHolder(lineShader, lineShaderParameterName, lineStride);
            data[(int)Indexes.GenericDot] = new MaterialAndBufferHolder(dotShader, dotShaderParameterName, dotStride);
            data[(int)Indexes.GenericLine] = new MaterialAndBufferHolder(lineShader, lineShaderParameterName, lineStride);
            data[(int)Indexes.GenericMesh] = new MaterialAndBufferHolder(trisShader, trisShaderParameterName, trisStride);

            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            //StartCoroutine(ClearDebug());
        }

        void Update() {
            OnUpdate();
            if (Debuger_K.settings.clearGenericOnUpdate)
                Debuger_K.ClearGeneric();
            //Debuger_K.AddDot(new Vector3(
            //    UnityEngine.Random.Range(0, 10.0f), 
            //    UnityEngine.Random.Range(0, 10.0f),
            //    UnityEngine.Random.Range(0, 10.0f)), 
            //    Color.red,
            //    UnityEngine.Random.Range(0.005f, 0.03f));

            //curTest++;
            //if(curTest > maxTest) {
            //    curTest = 0;
            //    Debuger_K.ClearDots();
            //}
        }
                
        void OnDisable() {
            ReleaseBuffer();
        }

        void OnDestroy() {
            EditorApplication.update -= OnUpdate;
            ReleaseBuffer();
        }

        void OnUpdate() {
            if (updateDelegate != null)
                updateDelegate.Invoke();
        }

        //IEnumerator ClearDebug() {
        //    while (true) {
        //        yield return new WaitForEndOfFrame();
        //        Debuger_K.ClearGeneric();
        //    }
        //}



    


        public void SetUpdateDeletage(Action UpdateDelegate) {
            updateDelegate = UpdateDelegate;
        }
        
        //important changes in big patches and dont changes very often
        public void UpdateImportantData(List<PointData> dotList, List<LineData> lineList, List<TriangleData> trisList) {
            lock(dotList)
                data[(int)Indexes.ImportantDot].UpdateBuffer(dotList.ToArray());

            lock(lineList)
                data[(int)Indexes.ImportantLine].UpdateBuffer(lineList.ToArray());

            lock(trisList)
                data[(int)Indexes.ImportantTris].UpdateBuffer(trisList.ToArray());
        }

        //path and generic in smaller patches
        public void UpdatePathData(List<LineData> lineList) {
            lock (lineList)
                data[(int)Indexes.Path].UpdateBuffer(lineList.ToArray());
        }
        public void UpdateGenericDots(List<PointData> dotList) {
            lock (dotList)
                data[(int)Indexes.GenericDot].UpdateBuffer(dotList.ToArray());
        }
        public void UpdateGenericLines(List<LineData> lineList) {
            lock (lineList)
                data[(int)Indexes.GenericLine].UpdateBuffer(lineList.ToArray());
        }
        public void UpdateGenericTris(List<TriangleData> trisList) {
            lock (trisList)
                data[(int)Indexes.GenericMesh].UpdateBuffer(trisList.ToArray());
        }

        private void ReleaseBuffer() {
            foreach (var d in data) {
                d.ReleaseBuffer();
            }          
        }

        void OnRenderObject() {
            for (int i = 0; i < 7; i++) {
                if(data[i].bufferLength > 0) {
                    data[i].material.SetPass(0);
                    Graphics.DrawProcedural(MeshTopology.Points, data[i].bufferLength);
                }
            }
        }

        class MaterialAndBufferHolder {
            public Material material;
            public ComputeBuffer buffer;
            public int bufferLength = 0;
            public string parameterName;
            public int stride;

            public MaterialAndBufferHolder(Shader shader, string ParameterName, int ParameterSize) {
                material = new Material(shader);
                parameterName = ParameterName;
                stride = ParameterSize;
            }

            public void UpdateBuffer(Array array) {      
                bufferLength = array.Length;

                if (buffer != null)
                    buffer.Release();

                if (bufferLength > 0) {
                    buffer = new ComputeBuffer(array.Length, stride);
                    buffer.SetData(array);
                    material.SetBuffer(parameterName, buffer);
                }
            }

            public void ReleaseBuffer(){
                if (buffer != null) {
                    buffer.Release();
                    buffer = null;
                }
            }
        }
    }
}

#endif