using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Satori.TimeServices;

namespace Satori.Kimai.Tests.Globals;

public class KimaiServiceCollection : ServiceCollection
{
    public KimaiServiceCollection()
    {
        var connectionSettings =  new ConnectionSettings
        {
            Url = new Uri("http://kimai.test/"),
            UserName = "me",
            ApiPassword = "myToken"
        };
        this.AddSingleton(connectionSettings);

        var mockHttp = new MockHttpMessageHandler();
        this.AddSingleton(mockHttp);
        this.AddSingleton(mockHttp.ToHttpClient());
        
        this.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);

        var timeServer = new TestTimeServer();
        this.AddSingleton(timeServer);
        this.AddSingleton<ITimeServer>(timeServer);

        this.AddSingleton<IKimaiServer, KimaiServer>();
    }
}