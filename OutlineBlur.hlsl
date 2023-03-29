#ifndef _INCLUDE_IMAGEBLUR_
#define _INCLUDE_IMAGEBLUR_

void GaussianBlur_float(sampler2D _MainTex, float2 uv, float blur, float _TextureWidth, float _TextureHeight, out fixed3 rgb, out fixed alpha)
{
    fixed4 col = fixed4(0.0, 0.0, 0.0, 0.0);
    float kernelSum = 0.0;

    blur = clamp(blur, 0, 20);
     
    int upper = ((blur - 1) / 2);
    int lower = -upper;
     
    for (int x = lower; x <= upper; ++x)
    {
        for (int y = lower; y <= upper; ++y)
        {
            kernelSum ++;

            float2 offset = float2(x / _TextureWidth, y / _TextureHeight);
            col += tex2D(_MainTex, uv + offset);
        }
    }
     
    col /= kernelSum;
    rgb = fixed3(col.r, col.g, col.b);
    alpha = col.a;
}

#endif