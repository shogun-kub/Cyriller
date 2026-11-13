using System.IO;
using System.Linq;
using Cyriller;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "text/html", "application/json" });
});
builder.Services.AddSingleton<CyrNounCollection>();
builder.Services.AddSingleton<CyrAdjectiveCollection>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/p500");
}

app.UseStatusCodePagesWithReExecute("/Error/p{0}");
app.UseResponseCompression();
app.UseStaticFiles();

string tempFolder = Path.Combine(app.Environment.ContentRootPath, "Temp");
Directory.CreateDirectory(tempFolder);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(tempFolder),
    RequestPath = "/Temp"
});

app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
