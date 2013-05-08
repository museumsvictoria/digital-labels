using IMu;

namespace DigitalLabels.Import.Factories
{
    public static class ManyNationsImuFactory
    {
        public static string[] GetImportColumns()
        {
            return new[]
                   {
                       "irn",
                       "ColRegPrefix",
                       "ColRegNumber",
                       "ColRegPart",
                       "ClaObjectName",
                       "DesLocalName",
                       "AdmDateModified",
                       "AdmTimeModified",
                       "materials=[MatPrimaryMaterials_tab,MatTertiaryMaterials_tab]",
                       "associations=[AssAssociationType_tab,AssAssociationDate_tab,name=AssAssociationNameRef_tab.(NamOtherNames_tab,NamOrganisation,NamPartyType),AssAssociationLocality_tab,AssAssociationState_tab,AssAssociationRegion_tab,MatTertiaryMaterials_tab]",
                       "narrative=<enarratives:ObjObjectsRef_tab>.(DetPurpose_tab,name=NarAuthorsRef_tab.NamFullName,NarNarrative,media=MulMultiMediaRef_tab.(irn,resource,MulMimeType,MdaDataSets_tab,MdaElement_tab,MdaQualifier_tab,MdaFreeText_tab,ChaRepository_tab,AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified),AdmPublishWebNoPassword,AdmDateModified,AdmTimeModified)",
                       "collobjs=<eexhibitobjects:StaObjectRef>.(irn,StaGridCode,StaSegmentName,StaCase)"
                   };
        }

        public static Terms GetImportTerms()
        {
            var terms = new Terms();

            terms.Add("MdaDataSets_tab", "Bunjilaka Digital Label");
            terms.Add("ClaSecondaryClassification", "Many Nations");
            terms.Add("AdmPublishWebNoPassword", "Yes");

            return terms;
        }
    }
}
