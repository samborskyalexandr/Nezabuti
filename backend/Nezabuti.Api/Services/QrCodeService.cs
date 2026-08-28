using Microsoft.Extensions.Options;
using Nezabuti.Api.Configuration;
using QRCoder;

namespace Nezabuti.Api.Services;

public interface IQrCodeService
{
    string GetCanonicalUrl(string publicId);
    byte[] GeneratePng(string publicId);
    string GenerateSvg(string publicId);
}

public sealed class QrCodeService : IQrCodeService
{
    private readonly AppPublicSettings _app;

    public QrCodeService(IOptions<AppPublicSettings> app)
    {
        _app = app.Value;
    }

    public string GetCanonicalUrl(string publicId)
    {
        var baseUrl = _app.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}/m/{publicId}";
    }

    public byte[] GeneratePng(string publicId)
    {
        var url = GetCanonicalUrl(publicId);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(8);
    }

    public string GenerateSvg(string publicId)
    {
        var url = GetCanonicalUrl(publicId);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        var qrCode = new SvgQRCode(data);
        return qrCode.GetGraphic(4);
    }
}
