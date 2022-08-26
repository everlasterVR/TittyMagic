using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static TittyMagic.Script;

namespace TittyMagic
{
    public static class Integration
    {
        private static List<JSONStorable> _otherInstances;

        public static List<JSONStorable> otherInstances => _otherInstances.Prune();

        public static void Init()
        {
            _otherInstances = new List<JSONStorable>();

            tittyMagic.NewJSONStorableAction("Integrate", RefreshOtherPluginInstances);
            RefreshOtherPluginInstances();

            /* When the plugin is added to an existing atom and this method gets called during initialization,
             * other instances are told to update their knowledge on what other instances are in the network.
             */
            foreach(var instance in _otherInstances)
            {
                if(instance.IsAction("Integrate"))
                {
                    instance.CallAction("Integrate");
                }
            }

            /* When the plugin is added as part of a new atom using e.g. scene or subscene merge load,
             * AddInstance adds the instance to this plugin's list of other instances.
             */
            SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
            SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
        }

        private static void RefreshOtherPluginInstances()
        {
            _otherInstances.Prune();
            SuperController.singleton.GetAtoms().ForEach(FindAndAddInstance);
        }

        private static void FindAndAddInstance(Atom atom)
        {
            if(atom.type != "Person" || atom.uid == tittyMagic.containingAtom.uid)
            {
                return;
            }

            JSONStorable otherInstance = null;
            try
            {
                var regex = new Regex(@"^plugin#\d+_TittyMagic.Script", RegexOptions.Compiled);
                otherInstance = atom
                    .GetStorableIDs()
                    .Where(id => regex.IsMatch(id))
                    .Select(atom.GetStorableByID)
                    .ToList()
                    .Prune()
                    .First(FindOtherInstanceStorable);
            }
            catch(Exception e)
            {
                Utils.LogError($"Error finding TittyMagic instance from Atom {atom.uid}: {e}");
            }

            if(!_otherInstances.Contains(otherInstance) && !_otherInstances.Exists(instance => instance.containingAtom.uid == atom.uid))
            {
                _otherInstances.Add(otherInstance);
            }
        }

        private static bool FindOtherInstanceStorable(JSONStorable storable)
        {
            bool result = false;
            if(storable.IsStringJSONParam("version"))
            {
                string versionString = storable.GetStringParamValue("version");
                if(versionString == $"{VERSION}")
                {
                    result = true;
                }
                else
                {
                    try
                    {
                        var version = new Version(storable.GetStringParamValue("version"));
                        if(version >= new Version("5.1.0"))
                        {
                            result = true;
                        }
                    }
                    catch(Exception e)
                    {
                        //ignore error raised by Version constructor for incorrect string value
                    }
                }
            }

            return result;
        }

        private static void OnAtomAdded(Atom atom)
        {
            _otherInstances.Prune();
            FindAndAddInstance(atom);
        }

        private static void OnAtomRemoved(Atom atom)
        {
            _otherInstances.Prune().RemoveAll(instance => instance.containingAtom.uid == atom.uid);
        }

        public static void RemoveHandlers()
        {
            SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
            SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
        }
    }
}
