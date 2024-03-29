using System;
using Gameplay.UI.Construction;
using TMPro;
using UnityEngine;

namespace Gameplay.UI.Construction
{
    public enum ConstructionType { TYPING, FILL_THE_BLANK }
    public interface IConstructionConsole { string queryString { get; } }
    public interface IFillTheBlankQuery : IConstructionConsole { void SetUpTokenField(Func<string, string[]> getOptionFunction, string tokens); }
    public interface ITypedQuery : IConstructionConsole { void startConsole(); }
}

namespace Gameplay.UI
{
    public interface IConstructionConsoleController
    {
        void SetUpOnYourOwnConsole();
        void SetUpTokenizeConsole(Func<string, string[]> getOptionFunction, string tokens);
        string queryString { get; }
    }

    public class ConstructionController : GameplayController, IConstructionConsoleController
    {
        [Header("Input Gameobjects")]
        [SerializeField] private GameObject _fillTheBlankGameobject;
        [SerializeField] private GameObject _typedQueryGameobject;

        private IFillTheBlankQuery _FTBController => mustGetComponent<IFillTheBlankQuery>(_fillTheBlankGameobject);
        private ITypedQuery _OnYourOwnController => mustGetComponent<ITypedQuery>(_typedQueryGameobject);

        [Header("Query text configuration")]
        [SerializeField] private string _defaultQuery;

        [Header("Input configuration")]
        [SerializeField] private TextMeshProUGUI _queryTextMesh;

        private ConstructionType _currentDisplayType;
        public string queryString
        {
            get => getQueryString(_currentDisplayType);
        }
        private void SetContructionType(ConstructionType type)
        {
            _fillTheBlankGameobject.SetActive(false);
            _typedQueryGameobject.SetActive(false);

            _currentDisplayType = type;
            switch (_currentDisplayType)
            {
                case ConstructionType.FILL_THE_BLANK:
                    _fillTheBlankGameobject.SetActive(true);
                    break;
                case ConstructionType.TYPING:
                    _typedQueryGameobject.SetActive(true);
                    break;
                default: throw new System.Exception(type.ToString() + " type is not yet implement or not existed");
            }
        }
        private string getQueryString(ConstructionType type)
        {
            switch (_currentDisplayType)
            {
                case ConstructionType.FILL_THE_BLANK: return _FTBController.queryString;
                case ConstructionType.TYPING: return _OnYourOwnController.queryString;
                default: throw new System.Exception(type.ToString() + " type is not yet implement or not existed");
            }
        }
        public void SetUpOnYourOwnConsole() 
        { 
            SetContructionType(ConstructionType.TYPING); 
            _OnYourOwnController.startConsole();
        }
        public void SetUpTokenizeConsole(Func<string, string[]> getOptionFunction, string tokens)
        {
            SetContructionType(ConstructionType.FILL_THE_BLANK);
            _FTBController.SetUpTokenField(getOptionFunction, tokens);
        }

        #region Unity Basic
        //Hide all console
        private void Start()
        {
            _fillTheBlankGameobject.SetActive(false);
            _typedQueryGameobject.SetActive(false);
        }
        #endregion
    }
}


