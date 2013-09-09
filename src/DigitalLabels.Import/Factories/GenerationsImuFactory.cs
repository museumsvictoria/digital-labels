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
        public static string[] GetPrimaryImportColumns()
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
                       "narrative=<enarratives:ObjObjectsRef_tab>.(irn,NarTitle,DetPurpose_tab,DepPeople0,NarNarrative,interviews=[name=IntIntervieweeRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),IntInterviewLocation_tab],AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)"
                   };
        }

        public static Terms GetPrimaryImportTerms()
        {
            var terms = new Terms();

            terms.Add("MdaDataSets_tab", "Bunjilaka Digital Label");
            terms.Add("ClaSecondaryClassification", "Generations");
            terms.Add("ClaObjectName", "primary");
            terms.Add("AdmPublishWebNoPassword", "Yes");
            
            return terms;
        }

        public static string[] GetSupportingImportColumns()
        {
            return new[]
                   {
                       "irn",
                       "DetPurpose_tab",
                       "DepPeople0",
                       "NarNarrative",
                       "interviews=[name=IntIntervieweeRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),IntInterviewLocation_tab]",
                       "master=AssMasterNarrativeRef.irn",
                       "AdmDateModified",
                       "AdmTimeModified",
                       "media=MulMultiMediaRef_tab.(irn,resource,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
                       "catalog=ObjObjectsRef_tab.(irn,ColRegPrefix,ColRegNumber,ColRegPart,ClaObjectName,ClaTertiaryClassification,AdmDateModified,AdmTimeModified,captions=[DesCaption_tab,DesPurpose_tab],associations=[AssAssociationType_tab,AssAssociationDate_tab,name=AssAssociationNameRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),AssAssociationLocality_tab,AssAssociationState_tab])"
                   };
        }

        public static Terms GetSupportingImportTerms()
        {
            var terms = new Terms();

            terms.Add("DetPurpose_tab", "Exhibition - Bunjilaka Generations Digital Label");
            terms.Add("NarTitle", "supporting");
            terms.Add("AdmPublishWebNoPassword", "Yes");

            return terms;
        }
    }
}
