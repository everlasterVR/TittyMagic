using System.Collections.Generic;
using Battlehub.RTCommon;
using Battlehub.RTSaveLoad;
using UnityEngine;

namespace TittyMagic.Diagnostics
{
    public class DrawService : IGL
    {
        private readonly Material _drawMat;
        public List<Vector3[]> lines { get; set; }

        private readonly Color _lineColor;

        public DrawService(Color lineColor)
        {
            var shader = Shader.Find("Battlehub/RTHandles/VertexColor");
            _drawMat = new Material(shader);
            _drawMat.hideFlags = HideFlags.HideAndDontSave;
            _drawMat.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            _drawMat.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _drawMat.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
            _drawMat.SetInt("_ZWrite", 0);
            lines = new List<Vector3[]>();
            _lineColor = lineColor;
        }

        public void Init()
        {
            if(GLRenderer.Instance == null)
            {
                new GameObject { name = "GLRenderer" }.AddComponent<GLRenderer>();
            }

            GLRenderer.Instance.Add(this);
        }

        public void Draw(int cullingMask)
        {
            DrawLines();
        }

        private void DrawLines()
        {
            GL.PushMatrix();
            _drawMat.SetPass(0);

            GL.Begin(GL.LINES);
            foreach(var line in lines)
            {
                GL.Color(_lineColor);
                GL.Vertex(line[0]);
                GL.Vertex(line[1]);
            }

            GL.End();
            GL.PopMatrix();
        }

        public void Remove()
        {
            GLRenderer.Instance.Remove(this);
        }
    }
}
