using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;

public class SessionTestModel : PageModel
{
    public int Counter { get; private set; }

    public void OnGet()
    {
        var value = HttpContext.Session.GetInt32("counter");

        if (value == null)
        {
            Counter = 1;
        }
        else
        {
            Counter = value.Value + 1;
        }

        HttpContext.Session.SetInt32("counter", Counter);
    }
}