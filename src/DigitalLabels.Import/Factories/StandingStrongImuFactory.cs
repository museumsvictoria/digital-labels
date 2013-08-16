using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMu;

namespace DigitalLabels.Import.Factories
{
    public static class StandingStrongImuFactory
    {
        public static string[] GetImportColumns()
        {
            return new[]
                {
                    "irn",
                    "NarTitle",
                    "NarNarrative",
                    "interviews=[name=IntIntervieweeRef_tab.(NamFullName),IntInterviewLocation_tab,IntInterviewDate0,IntInterviewNotes_tab]",
                    "media=MulMultiMediaRef_tab.(irn,resource,MulTitle,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
                    "AdmDateModified",
                    "AdmTimeModified"
                };
        }

        public static Terms GetImportTerms()
        {
            var terms = new Terms();

            terms.Add("DetPurpose_tab", "Exhibition - Bunjilaka Standing Strong Digital Label");
            terms.Add("AdmPublishWebNoPassword", "Yes");

            return terms;
        }
    }
}