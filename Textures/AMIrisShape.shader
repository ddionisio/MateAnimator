Shader "Animator/AMIrisShape" {
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
			SetTexture [_Mask] { matrix [_Matrix] combine texture }
			SetTexture [_MainTex] { ConstantColor [_Color] matrix[_TexMatrix] combine texture * constant, previous }
		}
	}
}