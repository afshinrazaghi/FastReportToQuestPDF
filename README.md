Lib To Generate PDF From FastReport by QuestPDF

## 1- Register Interface And Implementation
services.AddScoped<IPdfGenerator, PdfGenerator>();

##Inject
public class Test{
  private readonly IPdfGenerator _pdfGenerator;

  public Test(IPdfGenerator pdfGenerator){
    _pdfGenerator = pdfGenerator;
  }
}

##  Register Font
 _pdfGenerator.RegisterFont(Path.Combine("Files", "Fonts", "B Zar.TTF"));
 _pdfGenerator.RegisterFont(Path.Combine("Files", "Fonts", "SZARBD_0.ttf"));
 _pdfGenerator.RegisterFont(Path.Combine("Files", "Fonts", "NotoSansArabic-Regular.ttf"));
 _pdfGenerator.RegisterFont(Path.Combine("Files", "Fonts", "NotoSansArabic-Bold.ttf"));

## Register Custom Font
_pdfGenerator.RegisterCustomFont("B Zar",Path.Combine("Files", "Fonts", "B Zar.TTF"));

## Generate Pdf
var pdfExport = _pdfGenerator.GeneratePDF(report); 
Or
var pdfExport = _pdfGenerator.GeneratePDF(report, "NotoSansArabic"); 
