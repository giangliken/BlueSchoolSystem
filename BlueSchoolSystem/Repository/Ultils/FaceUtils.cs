using System.Security.Claims;

namespace BlueSchoolSystem.Utils;
public static class FaceUtils
{
    public static string GetUserId(this ClaimsPrincipal u)
    {
        var id =
            u.FindFirstValue("userId");                  

        if (string.IsNullOrWhiteSpace(id))
            throw new UnauthorizedAccessException("No user id in token");
        return id!;
    }

    public static byte[] FloatsToBytes(float[] arr)
    {
        var bytes = new byte[arr.Length * sizeof(float)];
        Buffer.BlockCopy(arr, 0, bytes, 0, bytes.Length);
        return bytes;
    }
    public static float[] BytesToFloats(byte[] bytes)
    {
        var arr = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, arr, 0, bytes.Length);
        return arr;
    }

    // Tự L2-normalize để tránh client gửi sai chuẩn
    public static float[] L2(float[] v)
    {
        float sum = 0; for (int i = 0; i < v.Length; i++) sum += v[i] * v[i];
        var norm = MathF.Sqrt(sum);
        if (norm <= 0) return v.ToArray();
        var o = new float[v.Length];
        for (int i = 0; i < v.Length; i++) o[i] = v[i] / norm;
        return o;
    }

    public static float Cosine(float[] a, float[] b)
    {
        // a,b đã L2 → cosine = dot
        var an = L2(a); var bn = L2(b);
        float dot = 0;
        int n = Math.Min(an.Length, bn.Length);
        for (int i = 0; i < n; i++) dot += an[i] * bn[i];
        return dot;
    }
}
