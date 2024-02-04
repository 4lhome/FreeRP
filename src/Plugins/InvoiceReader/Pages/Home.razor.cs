using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace InvoiceReader.Pages
{
    public partial class Home
    {
        [Inject]
        public FreeRP.Net.Client.Translation.I18nService I18nService { get; set; } = default!;

        [Inject]
        public FreeRP.Net.Client.Services.ConnectService ConnectService { get; set; } = default!;

        [Inject]
        public FreeRP.Net.Client.Services.UserService UserService { get; set; } = default!;

        [Inject]
        public FreeRP.Net.Client.Services.PdfService PdfService { get; set; } = default!;

        [Inject]
        public FreeRP.Net.Client.Services.DatabaseService DatabaseService { get; set; } = default!;

        [Inject]
        public FreeRP.Net.Client.Services.ContentService IOService { get; set; } = default!;

        [Inject]
        public FreeRP.Net.Client.Dialog.IBusyDialogService Busy { get; set; } = default!;

        [Inject]
        public IDialogService Dialog { get; set; } = default!;

        private readonly List<Data.InvoiceImporter> invoiceImporters = [];
        FluentInputFile? myFileByStream = default!;

        int progressPercent;

        const int fileSizeInMb = 20;
        readonly int fileSizeInB = fileSizeInMb * 1024 * 1024;
        readonly int fileCount = 10;
        readonly string accept = ".pdf";

        private readonly string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1lIjoidGVzdDJAdGVzdC5kZSIsIm5iZiI6MTcwMTU1ODI4NywiZXhwIjoxNzA0MjM2NDAwLCJpYXQiOjE3MDE1NTgyODd9.qZOmUuHGMwmmwabpdhJCmcLy-w3u8FyaE9i5xNcfIKo";
        private readonly string serverUrl = "https://localhost:7127";
        private readonly string _databaseId = "Database.InvoiceReader.TWyTec.com";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Busy.ShowBusyAsync(I18nService.Text.ConnectToServer);

                if (await ConnectService.TryConnectAsync(serverUrl))
                {
                    if (await UserService.LoginWithToken(token))
                    {
                        await Busy.CloseBusyAsync();
                    }
                    else
                    {
                        await Busy.CloseBusyAsync();
                        var dlgRef = await Dialog.ShowErrorAsync(I18nService.Text.LoginError);
                        await dlgRef.Result;
                    }
                }
                else
                {
                    await Busy.CloseBusyAsync();
                    var dlgRef = await Dialog.ShowErrorAsync(I18nService.Text.ConnectToServerError);
                    await dlgRef.Result;
                }
            }
        }

        private void GetData()
        {
            
        }

        async Task OnCompletedAsync(IEnumerable<FluentInputFileEventArgs> files)
        {
            List<Data.InvoiceImporter> list = [];

            foreach (var file in files)
            {
                progressPercent = file.ProgressPercent;
                
                if (file.Stream is not null)
                {
                    Data.InvoiceImporter import = new() { FileName = file.Name };

                    MemoryStream ms = new();
                    await file.Stream.CopyToAsync(ms);
                    await file.Stream.DisposeAsync();

                    import.FileAsBase64 = Convert.ToBase64String(ms.ToArray());
                    await ms.DisposeAsync();

                    var images = await PdfService.PdfToImagesAsync(import.FileAsBase64);
                    if (images is not null)
                    {
                        for (int i = 0; i < images.Length; i++)
                        {
                            import.Images.Add(i, images[i]);
                        }
                    }

                    list.Add(import);
                }
            }

            progressPercent = 0;

            if (list.Count > 0)
            {
                var dlgRef = await Dialog.ShowConfirmationAsync(I18nService.Text.XSplitOrMerge.Replace("{0}", "PDF"), I18nService.Text.Yes, I18nService.Text.No);
                var dlgRes = await dlgRef.Result;
                if (dlgRes.Cancelled == false)
                {

                }

                foreach (var imp in list)
                {
                    //MemoryStream ms = new(Convert.FromBase64String(imp.FileAsBase64));
                    //FreeRP.GrpcService.File.FrpFile frpFile = new() { 
                    //    Uri = $"db://{_databaseId}/{imp.FileName}"
                    //};
                    //var res = await IOService.UploadFileAsync(ms, frpFile, true);


                }
            }
        }
    }
}
