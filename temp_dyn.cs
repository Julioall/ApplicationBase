public class DynTest {
    public static void Try() {
        dynamic payload = new TokenResponseDto();
        try { var x = payload.token; }
        catch(Exception ex) { Console.WriteLine(ex.GetType().FullName); Console.WriteLine(ex.Message); }
        dynamic payload2 = new object();
        try { var y = payload2.token; }
        catch(Exception ex) { Console.WriteLine(ex.GetType().FullName); Console.WriteLine(ex.Message); }
    }
}
