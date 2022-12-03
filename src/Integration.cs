using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static TittyMagic.Script;

namespace TittyMagic
{
    public static class Integration
    {
        private static string _containingAtomUid;
        private static List<JSONStorable> _tittyMagicInstances = new List<JSONStorable>();
        private static List<JSONStorable> _bootyMagicInstances = new List<JSONStorable>();
        public static IEnumerable<JSONStorable> otherInstances => _tittyMagicInstances.Prune().Concat(_bootyMagicInstances.Prune());

        public static JSONStorable bootyMagic => _bootyMagicInstances
            .Prune()
            .FirstOrDefault(instance => instance.containingAtom.uid == _containingAtomUid);

        public static void Init()
        {
            /* Needs to be initialized with stored uid because tittyMagic.containingAtom.uid is unreachable OnDisable */
            _containingAtomUid = tittyMagic.containingAtom.uid;
            tittyMagic.NewJSONStorableAction(Constant.INTEGRATE, Integrate);
            Integrate();

            /* When the plugin is added to an existing atom and this method gets called during initialization,
             * other instances are told to update their knowledge on what other instances are in the network.
             */
            foreach(var instance in _tittyMagicInstances.Concat(_bootyMagicInstances))
            {
                instance.CallActionNullSafe(Constant.INTEGRATE);
            }

            /* When the plugin is added as part of a new atom using e.g. scene or subscene merge load,
             * AddInstance adds the instance to this plugin's list of other instances.
             */
            SuperController.singleton.onAtomAddedHandlers += OnAtomAdded;
            SuperController.singleton.onAtomRemovedHandlers += OnAtomRemoved;
        }

        private static void Integrate()
        {
            _tittyMagicInstances.Prune();
            _bootyMagicInstances.Prune();
            GetAtomsByType("Person").ForEach(FindAndAddOtherPluginStorables);
        }

        private static void FindAndAddOtherPluginStorables(Atom atom)
        {
            /* Find TittyMagic instance on atom */
            if(atom.uid != _containingAtomUid)
            {
                try
                {
                    var regex = new Regex($@"^plugin#\d+_{nameof(TittyMagic)}.{nameof(Script)}", RegexOptions.Compiled);
                    var otherPlugin = atom.FindStorablesByRegexMatch(regex)
                        .FirstOrDefault(storable => MinVersionStorableValue(storable, "5.1.0"));
                    if(otherPlugin != null &&
                        !_tittyMagicInstances.Contains(otherPlugin) &&
                        !_tittyMagicInstances.Exists(instance => instance.containingAtom.uid == atom.uid))
                    {
                        _tittyMagicInstances.Add(otherPlugin);
                    }
                }
                catch(Exception e)
                {
                    Utils.LogError($"Error finding {nameof(TittyMagic)} instance on Atom {atom.uid}: {e}");
                }
            }

            /* Find BootyMagic instance on atom */
            try
            {
                var regex = new Regex(@"^plugin#\d+_BootyMagic.Script", RegexOptions.Compiled);
                var otherPlugin = atom.FindStorablesByRegexMatch(regex)
                    .FirstOrDefault(storable => MinVersionStorableValue(storable, "1.0.0"));
                if(otherPlugin != null &&
                    !_bootyMagicInstances.Contains(otherPlugin) &&
                    !_bootyMagicInstances.Exists(instance => instance.containingAtom.uid == atom.uid))
                {
                    _bootyMagicInstances.Add(otherPlugin);
                }
            }
            catch(Exception e)
            {
                Utils.LogError($"Error finding BootyMagic instance on Atom {atom.uid}: {e}");
            }
        }

        private static bool MinVersionStorableValue(JSONStorable storable, string ver)
        {
            if(storable.IsStringJSONParam(Constant.VERSION))
            {
                string version = storable.GetStringParamValue(Constant.VERSION);
                try
                {
                    /* split version number from pre-release tag */
                    return new Version(version.Split('-')[0]) >= new Version(ver);
                }
                catch(Exception e)
                {
                    //ignore error raised by Version constructor for incorrect string value
                }
            }

            return false;
        }

        private static List<Atom> GetAtomsByType(string type)
            => SuperController.singleton.GetAtoms().Where(atom => atom.type == type).ToList();

        private static void OnAtomAdded(Atom atom)
        {
            _tittyMagicInstances.Prune();
            _bootyMagicInstances.Prune();
            FindAndAddOtherPluginStorables(atom);
        }

        private static void OnAtomRemoved(Atom atom)
        {
            _tittyMagicInstances.Prune().RemoveAll(instance => instance.containingAtom.uid == atom.uid);
            _bootyMagicInstances.Prune().RemoveAll(instance => instance.containingAtom.uid == atom.uid);
        }

        public static void Destroy()
        {
            _tittyMagicInstances = null;
            _bootyMagicInstances = null;
            SuperController.singleton.onAtomAddedHandlers -= OnAtomAdded;
            SuperController.singleton.onAtomRemovedHandlers -= OnAtomRemoved;
        }

        public new static string ToString() =>
            $"TittyMagics:\n{string.Join("\n", _tittyMagicInstances.Select(x => $"  {InstanceString(x)}").ToArray())}" +
            $"\nBootyMagics:\n{string.Join("\n", _bootyMagicInstances.Select(x => $"  {InstanceString(x)}").ToArray())}";

        private static string InstanceString(JSONStorable instance)
        {
            if(instance == null)
            {
                return "null";
            }

            return $"{instance.containingAtom.uid}: {instance.storeId}";
        }
    }
}
