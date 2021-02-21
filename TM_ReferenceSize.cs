using System.Linq;

namespace everlaster
{
    class TM_ReferenceSize : MVRScript
    {
        private bool enableUpdate = true;
        private JSONStorableVector3 breastSize;
        private JSONStorableVector3 breastAltSize;
        private JSONStorableBool altMode;
        private JSONStorable objectScale;

        public override void Init()
        {
            if(containingAtom.type != "ISSphere")
            {
                SuperController.LogError($"Plugin is for use with 'ISSphere' atom, not '{containingAtom.type}'");
                return;
            }

            Atom parent = containingAtom.parentAtom;
            if(parent == null || parent.type != "Person")
            {
                SuperController.LogError($"Atom must be parented to a 'Person' type atom");
                return;
            }

            InitParentPluginReference(parent);
            objectScale = containingAtom.GetStorableByID("scale");
            objectScale.SetFloatParamValue("scale", 2f);
            containingAtom.GetStorableByID("AtomControl").SetBoolParamValue("collisionEnabled", false);
            altMode = NewToggle("altMode"); 

            RefreshSize();
        }

        void InitParentPluginReference(Atom parent)
        {
            string storableId = parent.GetStorableIDs()
                .Where(it => it.Contains("TittyMagic"))
                .First();
            JSONStorable tittyMagic = parent.GetStorableByID(storableId);
            breastSize = tittyMagic.GetVector3JSONParam("Breast size");
            breastAltSize = tittyMagic.GetVector3JSONParam("Breast alt size");
            if (breastSize == null)
            {
                enableUpdate = false;
            }
        }

        JSONStorableBool NewToggle(string paramName)
        {
            JSONStorableBool storable = new JSONStorableBool(paramName, false);
            CreateToggle(storable, false);
            RegisterBool(storable);
            return storable;
        }

        public void Update()
        {
            if (enableUpdate)
            {
                RefreshSize();
            }
        }

        void RefreshSize()
        {
            if (altMode.val)
            {
                objectScale.SetFloatParamValue("scaleX", breastAltSize.val.x);
                objectScale.SetFloatParamValue("scaleY", breastAltSize.val.y);
                objectScale.SetFloatParamValue("scaleZ", breastAltSize.val.z);
            }
            else
            {
                objectScale.SetFloatParamValue("scaleX", breastSize.val.x);
                objectScale.SetFloatParamValue("scaleY", breastSize.val.y);
                objectScale.SetFloatParamValue("scaleZ", breastSize.val.z);
            }
        }
    }
}
