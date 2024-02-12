﻿using Assets.Scripts.DataPersistence.DialogController;
using Assets.Scripts.DataPersistence.SQLComponent;
using System.Linq;
using UnityEngine;
using Assets.Scripts.DataPersistence.ImageController;
using System.IO;
using Assets.Scripts.ScriptableObjects;
using Assets.Scripts.Helper;
using Assets.Scripts.DataPersistence.StepController;
using Assets.Scripts.DataPersistence.PuzzleManager;
using Assets.Scripts.DataPersistence.MissionStatusDetail;
using System;
using Gameplay;

namespace Assets.Scripts.DataPersistence
{
    public class MissionGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject _dialogConGameObject;
        [SerializeField] private GameObject _stepControllerGameObject;
        [SerializeField] private GameObject _puzzleManagerGameObject;
        [SerializeField] private GameObject _imageControllerGameObject;
        [SerializeField] private GameObject _missionControllerGameObject;
        [SerializeField] private GameObject _gameplayManagerGameObjefct;
        [SerializeField] private MissionData _missionSceneData;
        [SerializeField] private SelectedChapterData _selectedChapterData;

        private MissionConfig _missionConfig;
        private ISQLService _sqlService = new SQLService();
        private FileSystemWatcher _missionStatusFileWatcher;
        private FileSystemWatcher _chapterStatusFileWatcher;

        private void _StartGenerating()
        {
            LoadConfigFile();

            if (!_missionSceneData.IsPassed && _missionConfig.MissionType != MissionType.Placement)
            {
                InitiateMissionStatusFileWatcher();
            }

            if (!_selectedChapterData.IsPassed || _missionConfig.MissionType == MissionType.Placement)
            {
                InitiateChapterStatusFileWatcher();
            }

            LoadDialogController();
            LoadStepController();
            LoadPuzzleManager();
            LoadImageController();
            _InitiateMissionController();
        }

        #region Method for StartGenerating

        private void LoadConfigFile()
        {
            string folderPathAfterResources = _missionSceneData.MissionConfigFolderFullPath.Split(new string[] { "Resources/" }, StringSplitOptions.None)[1];
            TextAsset missionConfigFile = Resources.Load<TextAsset>(folderPathAfterResources + "/" + _missionSceneData.MissionFileName);
            _missionConfig = JsonUtility.FromJson<MissionConfig>(missionConfigFile.text);
        }

        private void InitiateMissionStatusFileWatcher()
        {
            _missionStatusFileWatcher = new FileSystemWatcher(_missionSceneData.MissionConfigFolderFullPath, EnvironmentData.Instance.StatusFileName + EnvironmentData.Instance.ConfigFileType);
            InitiateFileWatcher(_missionStatusFileWatcher);
        }

        private void InitiateChapterStatusFileWatcher()
        {
            _chapterStatusFileWatcher = new FileSystemWatcher(_selectedChapterData.ChapterFolderFullPath, EnvironmentData.Instance.StatusFileName + EnvironmentData.Instance.ConfigFileType);
            InitiateFileWatcher(_chapterStatusFileWatcher);
        }

        private void InitiateFileWatcher(FileSystemWatcher fileWatcher)
        {
            fileWatcher.NotifyFilter = NotifyFilters.CreationTime
                     | NotifyFilters.LastWrite
                     | NotifyFilters.Size;

            fileWatcher.EnableRaisingEvents = true;
        }

        private void LoadDialogController()
        {
            IDialogController dialogController = _dialogConGameObject.GetComponent<IDialogController>();
            dialogController.SetAllDialog(_missionConfig.MissionDetail.Where(x => x.Step == Step.Dialog).Select(x => x.Dialog).ToArray());
        }

        private void LoadStepController()
        {
            IStepController stepController = _stepControllerGameObject.GetComponent<IStepController>();
            Step[] allConfigStep = _missionConfig.MissionDetail.Select(x => x.Step).ToArray();

            int dialogIndex = 0;
            int puzzleIndex = 0;
            GameStep[] allGameStep = new GameStep[allConfigStep.Length + 1]; // Add slot for end step.
            for (int i = 0; i < allConfigStep.Length; i++)
            {
                Step step = allConfigStep[i];
                switch (step)
                {
                    case Step.Dialog:
                        allGameStep[i] = new GameStep(Step.Dialog, i, dialogIndex, -1);
                        dialogIndex++;
                        break;
                    case Step.Puzzle:
                        allGameStep[i] = new GameStep(Step.Puzzle, i, -1, puzzleIndex);
                        puzzleIndex++;
                        break;
                    default:
                        break;
                }
            }

            int lastStepIndex = allGameStep.Length - 1;
            allGameStep[lastStepIndex] = new GameStep(Step.EndStep, lastStepIndex, -1, -1);

            stepController.SetAllGameStep(allGameStep);
        }

        private void LoadPuzzleManager()
        {
            IPuzzleManager puzzleManager = _puzzleManagerGameObject.GetComponent<IPuzzleManager>();
            StepDetail[] allPuzzleStepDetail = _missionConfig.MissionDetail.Where(x => x.Step == Step.Puzzle).ToArray();
            PuzzleController.PuzzleController[] allPuzzleController = new PuzzleController.PuzzleController[allPuzzleStepDetail.Length];

            for(int i = 0; i < allPuzzleStepDetail.Length; i++)
            {
                StepDetail puzzleStepDetail = allPuzzleStepDetail[i];
                // 1) Create database path
                string dbFolder = $"/Resources/{EnvironmentData.Instance.DatabaseRootFolder}/";
                string dbConn = "URI=file:" + Application.dataPath + dbFolder + puzzleStepDetail.PuzzleDetail.DB;
                // 2) Get schema from SQLService
                Schema[] schemas = _sqlService.GetSchemas(dbConn, puzzleStepDetail.PuzzleDetail.Tables, false);
                // 3) Create PuzzleController
                PuzzleController.PuzzleController puzzleController = new PuzzleController.PuzzleController(dbConn, puzzleStepDetail.PuzzleDetail.AnswerSQL, puzzleStepDetail.Dialog, schemas, _sqlService, puzzleStepDetail.PuzzleDetail.VisualType, puzzleStepDetail.PuzzleDetail.BlankOptions, puzzleStepDetail.PuzzleDetail.PreSQL, puzzleStepDetail.PuzzleDetail.PuzzleType, puzzleStepDetail.PassedChapterID, puzzleManager);
                // 4) Insert PuzzleController to array.
                allPuzzleController[i] = puzzleController;
            }
            // 5) Insert all PuzzleController to PuzzleManager
            puzzleManager.Construct(allPuzzleController);
        }

        private void LoadImageController()
        {
            IImageController imageController = _imageControllerGameObject.GetComponent<IImageController>();
            string rootImgFolderPath = $"\\Resources\\{EnvironmentData.Instance.PuzzleImagesRootFolder}/";
            // Each index mean each step. Example imagePathLists[0] mean image for Step[0].
            string[][] imagePathLists = new string[_missionConfig.MissionDetail.Length][];

            for (int i = 0; i < _missionConfig.MissionDetail.Length; i++)

            {
                StepDetail stepDetail = _missionConfig.MissionDetail[i];

                if(stepDetail.ImgDetail != null)
                {
                    if (stepDetail.ImgDetail.ImgList.Length == 0)
                    {
                        DirectoryInfo di = new DirectoryInfo(Application.dataPath + rootImgFolderPath + stepDetail.ImgDetail.ImgFolder);

                        // Check if image path is correct.
                        try {
                            if (di.Exists)
                            {
                                FileInfo[] images = di.GetFiles("*.png");
                                imagePathLists[i] = images.Select(x => x.FullName).ToArray();

                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("Image folder path is not correct.");
                            Debug.LogWarning("Image folder path: " + di.FullName);
                            Debug.LogWarning("Because: " + e.Message);
                        }
                        
                    }
                    else
                    {
                        string[] imagePaths = stepDetail.ImgDetail.ImgList.Select(x => Application.dataPath + rootImgFolderPath + stepDetail.ImgDetail.ImgFolder + "\\" + x).ToArray();
                        imagePathLists[i] = new string[imagePaths.Length];
                        for (int j = 0; j < imagePaths.Length; j++) 
                        {
                            string imagePath = imagePaths[j];
                            if (!File.Exists(imagePath))
                            {
                                Debug.LogWarning("Image path is not correct.");
                                Debug.LogWarning("Image path: " + imagePath);
                            }
                            else
                            {
                                imagePathLists[i][0] = imagePath;
                            }
                        }
                    }

                }
            }

            imageController.SetImagesList(imagePathLists);
        }

        private void _InitiateMissionController()
        {
            MissionController missioncontroller = _missionControllerGameObject.GetComponent<MissionController>();
            missioncontroller.Initiate(_missionSceneData.MissionConfigFolderFullPath, _missionConfig.MissionID, _missionConfig.MissionType, new SaveManager.SaveManager(), _missionSceneData.IsPassed, _selectedChapterData.IsPassed);
        }
        #endregion

        private void _StartGamePlay()
        {
            //Start gameplay after mission generated.
            IGameplayManager gameplayManager = _gameplayManagerGameObjefct.GetComponent<IGameplayManager>();
            if (_missionStatusFileWatcher != null && _chapterStatusFileWatcher != null)
            {
                gameplayManager.StartFinalGameplay(_missionStatusFileWatcher, _chapterStatusFileWatcher);
            }
            else if (_missionStatusFileWatcher != null && _chapterStatusFileWatcher == null)
            {
                gameplayManager.StartNormalGameplay(_missionStatusFileWatcher);
            }
            else if (_missionStatusFileWatcher == null && _chapterStatusFileWatcher != null)
            {
                gameplayManager.StartPlacement(_chapterStatusFileWatcher);
            }
            else
            {
                gameplayManager.StartFreeGame();
            }
        }

        // Use this for initialization
        void Start()
        {
            _StartGenerating();
            _StartGamePlay();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}