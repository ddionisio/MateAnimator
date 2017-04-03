Shader "M8/Animator/AMIrisShape" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    	_Mask ("Culling Mask", 2D) = "white" {}
    	_Cutoff ("Alpha cutoff", Range (0,1)) = 0.1
     	_Color ("Main Color", Color) = (1,1,1,1)
    }

    SubShader {
    	Tags {"Queue"="Background"}
    	Lighting Off
    	ZWrite Off
    	Blend SrcAlpha OneMinusSrcAlpha
    	AlphaTest GEqual [_Cutoff]
    	Pass {
            CGPROGRAM
            #pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Mask;
            fixed4 _Color;
            float4x4 _Matrix;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvm : TEXCOORD1;
            };
            
            v2f vert(float4 pos : POSITION, float2 uv : TEXCOORD0) {
                v2f o;
                o.pos = UnityObjectToClipPos(pos);
                o.uv = uv;
                o.uvm = mul(_Matrix, float4(uv, 0, 1)).xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return fixed4((tex2D(_MainTex, i.uv) * _Color).rgb, tex2D(_Mask, i.uvm).a);
            }
            ENDCG
    	}
    }
}