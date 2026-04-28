using System.Text;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;

public class SimpleImageReceiver : DicomService, IDicomServiceProvider, IDicomCStoreProvider
{
    public SimpleImageReceiver(
        INetworkStream stream,
        Encoding fallbackEncoding,
        ILogger log,
        DicomServiceDependencies deps
    )
        : base(stream, fallbackEncoding, log, deps) { }

    public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
    {
        Console.WriteLine("Association received");

        // IMPORTANT: accept everything for testing
        foreach (var pc in association.PresentationContexts)
        {
            pc.AcceptTransferSyntaxes(
                DicomTransferSyntax.ExplicitVRLittleEndian,
                DicomTransferSyntax.ImplicitVRLittleEndian
            );
        }

        foreach (var pc in association.PresentationContexts)
        {
            Console.WriteLine($"{pc.AbstractSyntax} => {pc.Result}");
        }

        return Task.CompletedTask;
    }

    public void OnConnectionClosed(Exception exception)
    {
        Console.WriteLine($"Connection closed: {exception?.Message}");
    }

    public Task OnReceiveAssociationReleaseRequestAsync() => Task.CompletedTask;

    public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
    {
        throw new NotImplementedException();
    }

    public Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request)
    {
        Console.WriteLine("Image received!");

        var image = new DicomImage(request.File.Dataset);
        // var bitmap = image.RenderImage().AsClonedBitmap();
        //
        // var form = new Form
        // {
        //     Text = "DICOM Image",
        //     Width = bitmap.Width,
        //     Height = bitmap.Height
        // };
        //
        // form.Controls.Add(new PictureBox
        // {
        //     Dock = DockStyle.Fill,
        //     Image = bitmap,
        //     SizeMode = PictureBoxSizeMode.Zoom
        // });
        //
        // form.Show();

        return Task.FromResult(new DicomCStoreResponse(request, DicomStatus.Success));
    }

    public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e)
    {
        throw new NotImplementedException();
    }
}
