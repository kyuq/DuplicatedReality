Shader "Kinect4Azure/OcclusionShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Geometry"}
		
        ZWrite On
		ZTest LEqual
		ColorMask 0

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
			#pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

			uint _DepthWidth;
			uint _DepthHeight;
			uint _ColorWidth;
			uint _ColorHeight;
            float _MaxPointDistance;

            float4x4 _PointcloudOrigin;
            float4x4 _Col2DepCalibration;

            Texture2D<float> _DepthTex;
            Texture2D<float4> _ColorTex;
            Texture2D<float4> _XYLookup;

            SamplerState sampler_ColorTex
            {
                Filter = MIN_MAG_MIP_LINEAR;
                AddressU = Wrap;
                AddressV = Wrap;
            };

            struct appdata
            {
                float4 vertex : POSITION;
            };

			struct v2g
			{
				uint vid: VERTEXID;
			};

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 posWorld : TEXCOORD1;
            };

            v2g vert (uint vid: SV_VertexID)
            {
                v2g o;
				o.vid = vid;
                return o;
            }

			[maxvertexcount(6)]
			void geom(point v2g input[1], inout TriangleStream<g2f> outStream)
			{
				uint index = input[0].vid;

                // create a surface using left oriented quad
                uint x = index % _DepthWidth;
                uint y = index / _DepthWidth;

                if(x == _DepthWidth - 1 || y == _DepthHeight - 1) return;

                float3 texel0 = float3(x, y, 0);
                float3 texel1 = float3(x + 1, y, 0);
                float3 texel2 = float3(x, y + 1, 0);
                float3 texel3 = float3(x + 1, y + 1, 0);

                float depth0 = _DepthTex.Load(texel0) * 65536;
                float depth1 = _DepthTex.Load(texel1) * 65536;
                float depth2 = _DepthTex.Load(texel2) * 65536;
                float depth3 = _DepthTex.Load(texel3) * 65536;

                // filter out 0 values
                if (depth0 < 1 || depth1 < 1 || depth2 < 1 || depth3 < 1)
                    return;

                float4 xy0 = _XYLookup.Load(texel0) * 2 - 1;
                float4 xy1 = _XYLookup.Load(texel1) * 2 - 1;
                float4 xy2 = _XYLookup.Load(texel2) * 2 - 1;
                float4 xy3 = _XYLookup.Load(texel3) * 2 - 1;

                float4x4 OxC = mul(_PointcloudOrigin, _Col2DepCalibration);

                float4 pos0 = mul(OxC, float4(float3(xy0.x, -xy0.y , 1) * depth0 * 0.001f, 1.0f));
                float4 pos1 = mul(OxC, float4(float3(xy1.x, -xy1.y , 1) * depth1 * 0.001f, 1.0f));
                float4 pos2 = mul(OxC, float4(float3(xy2.x, -xy2.y , 1) * depth2 * 0.001f, 1.0f));
                float4 pos3 = mul(OxC, float4(float3(xy3.x, -xy3.y , 1) * depth3 * 0.001f, 1.0f));

                // filter out stretched surfaces
                if( distance(pos0, pos1) > _MaxPointDistance || 
                    distance(pos1, pos3) > _MaxPointDistance || 
                    distance(pos3, pos2) > _MaxPointDistance || 
                    distance(pos2, pos0) > _MaxPointDistance)
                    return;

                g2f o;
                o.posWorld = float4(0,0,0,0);

                o.pos = UnityObjectToClipPos(pos0);
                outStream.Append(o);
                
                o.pos = UnityObjectToClipPos(pos1);
                outStream.Append(o);

                o.pos = UnityObjectToClipPos(pos2);
                outStream.Append(o);

                o.pos = UnityObjectToClipPos(pos1);
                outStream.Append(o);

                o.pos = UnityObjectToClipPos(pos2);
                outStream.Append(o);

                o.pos = UnityObjectToClipPos(pos3);
                outStream.Append(o);

                outStream.RestartStrip();
			}

            fixed4 frag (g2f i) : SV_Target
            {
                return float4(1,1,1,1);
            }
            ENDCG
        }
    }
}
