namespace Mandelbrot;

public static class Utils {
    public static string GetStringFromBytes(byte[] bytes) {
        string res = "";
        
        foreach (byte b in bytes) {
            res += (char)b;
        }
        
        return res;
    }

    public static void WriteColorPair(BinaryWriter writer, Color a, Color b) {
        writer.Write(BitConverter.GetBytes((int)a.R)[0]);
        writer.Write(BitConverter.GetBytes((int)a.G)[0]);
        writer.Write(BitConverter.GetBytes((int)a.B)[0]);
        writer.Write(BitConverter.GetBytes((int)b.R)[0]);
        writer.Write(BitConverter.GetBytes((int)b.G)[0]);
        writer.Write(BitConverter.GetBytes((int)b.B)[0]);
    }
}