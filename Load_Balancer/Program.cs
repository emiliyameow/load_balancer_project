namespace Load_Balancer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/", () => "Hello Load Balancer!");

/*
 * йеь - щЛХКХЪ
 * 
 * юкцнпхрл аюкюмяхпнбйх - аНЦДЮМ
 * 
 * люпьпсрхгюжхъ - лЮРБЕИ
 * 
 * опнбепйю фхгмеяонянамнярх яепбепнб - ?
 * 
 * йнмтхцспюжхъ аюкюмяхпнбыхйю - ?
 */

        app.Run();
    }
}
