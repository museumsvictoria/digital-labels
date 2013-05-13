using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMu;

namespace DigitalLabels.Import.Factories
{
    public static class GenerationsImuFactory
    {
        public static string[] GetImportColumns()
        {
            return new[]
                   {
                       "irn",
                       "ColRegPrefix",
                       "ColRegNumber",
                       "ColRegPart",
                       "ClaTertiaryClassification",
                       "AdmDateModified",
                       "AdmTimeModified",
                       "captions=[DesCaption_tab,DesPurpose_tab]",
                       "associations=[AssAssociationType_tab,AssAssociationDate_tab,name=AssAssociationNameRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),AssAssociationLocality_tab,AssAssociationState_tab]",
                       "media=MulMultiMediaRef_tab.(irn,resource,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
                       "narrative=<enarratives:ObjObjectsRef_tab>.(irn,DetPurpose_tab,DepPeople0,NarNarrative,NarNarrativeSummary,interviews=[name=IntIntervieweeRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),IntInterviewLocation_tab],master=AssMasterNarrativeRef.irn,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)"
                   };
        }

        public static Terms GetImportTerms()
        {
            var terms = new Terms();

            terms.Add("MdaDataSets_tab", "Bunjilaka Digital Label");
            terms.Add("ClaSecondaryClassification", "Generations");
            terms.Add("AdmPublishWebNoPassword", "Yes");
            
            return terms;
        }
    }
}
