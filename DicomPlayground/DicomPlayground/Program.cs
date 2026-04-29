using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

new DicomSetupBuilder()
    .RegisterServices(s => s.AddFellowOakDicom().AddImageManager<WinFormsImageManager>())
    .Build();

var file = DicomFile.Open("CT_small.dcm");
var request = new DicomCStoreRequest(file);
var client = DicomClientFactory.Create("localhost", 4242, false, "SCU", "SCP");
await client.AddRequestAsync(request);
await client.SendAsync();

var studyUid = string.Empty;
request.OnResponseReceived += async (req, resp) =>
{
    Console.WriteLine($"Status: {resp.Status}- Test file stored successfully");
    var requestRead = CreateRequestRead();
    requestRead.OnResponseReceived += async (req, resp) =>
    {
        Console.WriteLine($"Study response: {resp.Status}");
        var ds = resp.Dataset;
        if (ds != null)
        {
            studyUid = resp.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
            Console.WriteLine($"Study UID: {studyUid}");
        }
    };
    await client.AddRequestAsync(requestRead);
    await client.SendAsync();
};
await client.AddRequestAsync(request);
await client.SendAsync();

//TODO JP: I should build this correctly async with async lambdas.

var seriesUid = string.Empty;
var seriesRequestRead = new DicomCFindRequest(DicomQueryRetrieveLevel.Series)
{
    Dataset = new DicomDataset
    {
        { DicomTag.QueryRetrieveLevel, "SERIES" },
        { DicomTag.StudyInstanceUID, studyUid },
        { DicomTag.SeriesInstanceUID, "" },
    },
};
seriesRequestRead.OnResponseReceived += (req2, resp2) =>
{
    Console.WriteLine($"Series response: {resp2.Status}");

    var seriesDataset = resp2.Dataset;
    if (seriesDataset != null)
    {
        seriesUid = seriesDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
        Console.WriteLine($"Study UID: {seriesUid}");
    }
};

await client.AddRequestAsync(seriesRequestRead);
await client.SendAsync();

var imageRequestRead = new DicomCFindRequest(DicomQueryRetrieveLevel.Image)
{
    Dataset = new DicomDataset
    {
        { DicomTag.QueryRetrieveLevel, "IMAGE" },
        { DicomTag.StudyInstanceUID, studyUid },
        { DicomTag.SeriesInstanceUID, seriesUid },
        { DicomTag.SOPInstanceUID, "" }, //Ids of the object, ImageID is an optional meta data field.
        { DicomTag.SOPClassUID, "" }, //type of the object, so i can find out if it is an image.
    },
};
var sopUid = string.Empty;
imageRequestRead.OnResponseReceived += (req3, resp3) =>
{
    Console.WriteLine($"Image response: {resp3.Status}");

    var imagesDataset = resp3.Dataset;
    if (imagesDataset != null)
    {
        var sopClassUid = imagesDataset.GetSingleValue<string>(DicomTag.SOPClassUID);
        if (sopClassUid == DicomUID.CTImageStorage.UID)
        {
            sopUid = imagesDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
        }

        Console.WriteLine($"SopUid of the first ctImage is : {sopUid}");
    }
};

await client.AddRequestAsync(imageRequestRead);
await client.SendAsync();

var cget = new DicomCGetRequest(studyUid, seriesUid, sopUid);
client.OnCStoreRequest += OnCStoreRequestAsync;
var pcs = DicomPresentationContext.GetScpRolePresentationContextsFromStorageUids(
    DicomStorageCategory.Image,
    DicomTransferSyntax.ExplicitVRLittleEndian,
    DicomTransferSyntax.ImplicitVRLittleEndian,
    DicomTransferSyntax.ImplicitVRBigEndian
);
client.AdditionalPresentationContexts.AddRange(pcs);
await client.AddRequestAsync(cget);
await client.SendAsync();

Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest cStoreRequest)
{
    Console.WriteLine("IMAGE RECEIVED!");

    var image = new DicomImage(cStoreRequest.File.Dataset);

    var bitmap = image.RenderImage().AsClonedBitmap();

    // show it or save it
    bitmap.Save("testDicomJP.png");

    return Task.FromResult(new DicomCStoreResponse(cStoreRequest, DicomStatus.Success));
}

DicomCFindRequest CreateRequestRead()
{
    return new DicomCFindRequest(DicomQueryRetrieveLevel.Study)
    {
        Dataset = new DicomDataset
        {
            { DicomTag.QueryRetrieveLevel, "STUDY" },
            { DicomTag.PatientName, "*" },
            { DicomTag.StudyInstanceUID, "" },
        },
    };
}
