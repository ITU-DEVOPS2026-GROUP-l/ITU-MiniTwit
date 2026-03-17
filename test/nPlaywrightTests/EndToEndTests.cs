using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace nPlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class EndToEndTests : PageTest
{
    private TestChirpWebFactory _factory = null!;
    private string _baseUrl = null!;

    [OneTimeSetUp]
    public async Task SetupFactory()
    {
        _factory = new TestChirpWebFactory();
        await _factory.StartAsync();
        _baseUrl = _factory.BaseAddress;
    }

    [Test]
    public async Task ChirpWebsiteExists()
    {
        var response = await Page.GotoAsync(_baseUrl);

        TestContext.Out.WriteLine($"Navigated to {response?.Url} with status {response?.Status}");
        if (response is { Status: not 200 })
        {
            TestContext.Out.WriteLine(await response.TextAsync());
        }

        Assert.That(response, Is.Not.Null, "No response returned from the web app.");
        Assert.That(response!.Status, Is.EqualTo(200), $"Home page returned status {response.Status}.");

        var title = await Page.TitleAsync();
        Assert.That(title, Is.EqualTo("Chirp!"));
    }

    [Test]
    public async Task newUserTest()
    {
        await Page.GotoAsync(_baseUrl);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Register as a new user" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("noah");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("noah@outlook.dk");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password", Exact = true }).FillAsync("Hej1234!");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Confirm Password" }).FillAsync("Hej1234!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
        //await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "logout [noah]" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task createCheepTest()
    {
        await Page.GotoAsync(_baseUrl);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("phqu@itu.dk");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("Dinmor123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Page.Locator("#Text").ClickAsync();
        await Page.Locator("#Text").FillAsync("halløj");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Share" }).ClickAsync();
        await Expect(Page.Locator("#messagelist")).ToContainTextAsync("philip");
        await Expect(Page.GetByText("halløj").First).ToBeVisibleAsync();
    }


    [Test]
    public async Task loginTest()
    {
        await Page.GotoAsync(_baseUrl);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("phqu@itu.dk");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("Dinmor123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Expect(Page.Locator("h3")).ToContainTextAsync("What's on your mind philip?");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "logout [philip]" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task followUserTest() 
    {
        await Page.GotoAsync(_baseUrl);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("phqu@itu.dk");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("Dinmor123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        var cheepItem = Page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Adhede" }).First;
        await cheepItem.GetByRole(AriaRole.Button, new() { Name = "Follow" }).ClickAsync();
        await Expect(cheepItem.GetByRole(AriaRole.Button, new() { Name = "Unfollow" })).ToBeVisibleAsync();
        await cheepItem.GetByRole(AriaRole.Button, new() { Name = "Unfollow" }).ClickAsync();
        await Expect(cheepItem.GetByRole(AriaRole.Button, new() { Name = "Follow" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task unfollowUserTest()
    {
        await Page.GotoAsync(_baseUrl);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("phqu@itu.dk");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("Dinmor123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        var cheepItem = Page.GetByRole(AriaRole.Listitem).Filter(new() { HasText = "Adhede" }).First;
        await cheepItem.GetByRole(AriaRole.Button, new() { Name = "Follow" }).ClickAsync();
        await Expect(cheepItem.GetByRole(AriaRole.Button, new() { Name = "Unfollow" })).ToBeVisibleAsync();
        await cheepItem.GetByRole(AriaRole.Button, new() { Name = "Unfollow" }).ClickAsync();
        await Expect(cheepItem.GetByRole(AriaRole.Button, new() { Name = "Follow" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task logoutTest()
    {
        await Page.GotoAsync(_baseUrl);
        await Page.Locator("div").Nth(1).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).FillAsync("phqu@itu.dk");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Email" }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync("Dinmor123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "logout [philip]" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Login" })).ToBeVisibleAsync();
    }

    [OneTimeTearDown]
    public async Task TearDownFactory()
    {
        await _factory.DisposeAsync();
    }
}
