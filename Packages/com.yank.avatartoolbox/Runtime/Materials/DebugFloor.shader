Shader "Yan-K/SceneControllerDebugFloor"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.196, 0.196, 0.196, 1)
        _LineColor ("Line Color", Color) = (0.471, 0.471, 0.471, 1)
        _MajorSpacing ("Major Line Spacing (m)", Float) = 1.0
        _MinorSpacing ("Minor Line Spacing (m)", Float) = 0.5
        _MajorThickness ("Major Line Width (×0.01 m)", Range(0, 10)) = 4
        _MinorThickness ("Minor Line Width (×0.01 m)", Range(0, 10)) = 2
        _MinorOpacity ("Minor Line Opacity", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            fixed4 _BaseColor;
            fixed4 _LineColor;
            float  _MajorSpacing;
            float  _MinorSpacing;
            float  _MajorThickness;
            float  _MinorThickness;
            float  _MinorOpacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Screen-space anti-aliased grid line with distance fade to kill
            // Moiré / flicker in the far field.
            // coord     : world-space coordinate (x or z)
            // spacing   : world-space distance between lines
            // thickness : world-space line width in METRES — lines thin with distance
            // Returns a 0..1 alpha — 1 on-line, 0 elsewhere.
            float LineMask(float coord, float spacing, float thickness)
            {
                float c = coord / spacing;
                // Use two-axis derivatives so pixels with coord running diagonally
                // don't underestimate thickness. fwidth == |ddx|+|ddy|.
                float fw = max(fwidth(c), 1e-5);

                // Distance (in line-space units) to the nearest grid line.
                float d = abs(frac(c - 0.5) - 0.5);

                // Half-width in line-space. The property is scaled ×0.01 so the
                // inspector slider (0–10) maps to 0–0.1 m world-space width.
                // Lines naturally thin with distance because this is world-space.
                float halfW = thickness * 0.01 * 0.5 / spacing;
                float lineA = 1.0 - smoothstep(halfW, halfW + fw, d);

                // Nyquist fade: when more than ~1 period fits in a pixel,
                // neighbouring lines alias together. Fade them out smoothly so
                // the far field settles to the base colour instead of shimmering.
                float fade = 1.0 - smoothstep(0.4, 0.8, fw);
                return lineA * fade;
            }

            // Radial linear/exp/exp2 fog — view-angle independent, so the floor
            // doesn't "swim" as the camera rotates.
            float RadialFogFactor(float3 worldPos)
            {
                float dist = distance(_WorldSpaceCameraPos, worldPos);
                // unity_FogParams = (density/sqrt(ln(2)), density/ln(2),
                //                    -1/(end-start),      end/(end-start))
                // Linear and Exp/Exp2 share the same uniform; pick based on keyword.
            #if defined(FOG_LINEAR)
                return saturate(unity_FogParams.z * dist + unity_FogParams.w);
            #elif defined(FOG_EXP)
                return saturate(exp2(-unity_FogParams.y * dist));
            #elif defined(FOG_EXP2)
                float f = unity_FogParams.x * dist;
                return saturate(exp2(-f * f));
            #else
                // No fog keyword active — fall back to Unity's RenderSettings.fog
                // flag via a manual linear-mode evaluation so a user who toggles
                // fog at runtime without recompiling still sees it.
                return saturate(unity_FogParams.z * dist + unity_FogParams.w);
            #endif
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float majorX = LineMask(i.worldPos.x, _MajorSpacing, _MajorThickness);
                float majorZ = LineMask(i.worldPos.z, _MajorSpacing, _MajorThickness);
                float major = saturate(max(majorX, majorZ));

                float minorX = LineMask(i.worldPos.x, _MinorSpacing, _MinorThickness);
                float minorZ = LineMask(i.worldPos.z, _MinorSpacing, _MinorThickness);
                float minor = saturate(max(minorX, minorZ)) * _MinorOpacity;

                // Compute fog first — we use it to fade lines too.
                float fogF = RadialFogFactor(i.worldPos);

                // Lines dissolve twice as fast as the base surface.
                // When fog is 50% the lines are already gone, so the far field
                // settles cleanly into the fog colour instead of shimmering.
                float lineFade = saturate(fogF * 2.0);

                // Major lines override minor lines so the grid reads cleanly.
                float lineAmt = saturate(max(major, minor)) * lineFade;

                fixed4 col = lerp(_BaseColor, _LineColor, lineAmt);
                col.a = 1.0;
                col.rgb = lerp(unity_FogColor.rgb, col.rgb, fogF);
                return col;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
