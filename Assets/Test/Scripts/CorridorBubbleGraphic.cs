using UnityEngine;

namespace DwtD.TestScene
{
    // Resolution-independent comic shapes; no texture generation or font symbols required.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CorridorBubbleGraphic : UnityEngine.UI.MaskableGraphic
    {
        public bool thoughtCloud;
        public Color borderColor=new Color(.15f,.26f,.29f,1);
        public float borderWidth=3;
        public float cornerRadius=24;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            Rect r=rectTransform.rect;
            if(r.width<=0 || r.height<=0) return;
            if(thoughtCloud)
            {
                Cloud(vh,r.center,new Vector2(r.width*.49f,r.height*.45f),borderColor);
                Cloud(vh,r.center,new Vector2(r.width*.49f-borderWidth,r.height*.45f-borderWidth),color);
                Dot(vh,r.center+new Vector2(-r.width*.28f,-r.height*.53f),13,borderColor);
                Dot(vh,r.center+new Vector2(-r.width*.28f,-r.height*.53f),10,color);
                Dot(vh,r.center+new Vector2(-r.width*.33f,-r.height*.66f),8,borderColor);
                Dot(vh,r.center+new Vector2(-r.width*.33f,-r.height*.66f),5,color);
            }
            else
            {
                Rounded(vh,r,borderColor,cornerRadius);
                Rect inner=new Rect(r.x+borderWidth,r.y+borderWidth,r.width-2*borderWidth,r.height-2*borderWidth);
                Rounded(vh,inner,color,Mathf.Max(0,cornerRadius-borderWidth));
            }
        }

        static void Cloud(UnityEngine.UI.VertexHelper vh,Vector2 center,Vector2 size,Color tint)
        {
            const int n=144;
            int first=vh.currentVertCount;
            vh.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<=n;i++)
            {
                float a=i*Mathf.PI*2/n;
                float scallop=1+.055f*Mathf.Cos(a*10);
                vh.AddVert(center+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y)*scallop,tint,Vector2.zero);
                if(i>0) vh.AddTriangle(first,first+i,first+i+1);
            }
        }
        static void Dot(UnityEngine.UI.VertexHelper vh,Vector2 center,float radius,Color tint)
        {
            int first=vh.currentVertCount;
            vh.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<=24;i++)
            {
                float a=i*Mathf.PI*2/24;
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,Vector2.zero);
                if(i>0) vh.AddTriangle(first,first+i,first+i+1);
            }
        }
        static void Rounded(UnityEngine.UI.VertexHelper vh,Rect r,Color tint,float radius)
        {
            float rad=Mathf.Min(radius,Mathf.Min(r.width,r.height)*.5f);
            int first=vh.currentVertCount;
            vh.AddVert(r.center,tint,Vector2.zero);
            for(int corner=0;corner<4;corner++)
            {
                Vector2 c=new Vector2(corner==0 || corner==3?r.xMax-rad:r.xMin+rad,corner<2?r.yMax-rad:r.yMin+rad);
                for(int j=0;j<=10;j++)
                {
                    float a=(corner*90+j*9)*Mathf.Deg2Rad;
                    vh.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad,tint,Vector2.zero);
                    int k=corner*11+j;
                    if(k>0) vh.AddTriangle(first,first+k,first+k+1);
                }
            }
            vh.AddTriangle(first,first+44,first+1);
        }
    }
}
