using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LeaveFlow.Tests;

internal static class ControllerTestHelper
{
    // 直接呼叫 controller action 不會經過 MVC pipeline，TempData 需手動掛上才能用
    public static void AttachTempData(Controller controller)
    {
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new NoopTempDataProvider());
    }

    private sealed class NoopTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
