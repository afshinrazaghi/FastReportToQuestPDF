using FastReport.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace FastReportToQuestPDF
{
    public interface IConvertor
    {
        byte[] GeneratePDF(WebReport report);
        byte[] GeneratePDF(WebReport report, string? unicodeFontName);
        byte[] GeneratePDF(WebReport report, string? unicodeFontName, string? emojiFontName);
        void RegisterFont(string fontPath);
        public void RegisterCustomFont(string fontName, string fontPath);

    }
}
