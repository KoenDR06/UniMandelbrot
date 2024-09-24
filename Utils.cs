namespace Mandelbrot;

public static class Utils {
    public static string GetStringFromBytes(byte[] bytes) {
        string res = "";
        
        foreach (byte b in bytes) {
            res += (char)b;
        }
        
        return res;
    }
}