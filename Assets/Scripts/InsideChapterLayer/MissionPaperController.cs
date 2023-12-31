﻿using UnityEngine;

namespace Assets.Scripts.InsideChapterLayer
{
    public class MissionPaperController : MonoBehaviour
    {
        private string _missionConfigFolderPath;
        private string _missionFileName;
        [SerializeField] private MissionManager _missionManager;

        /// <summary>
        /// Construct mission controller completely.
        /// </summary>
        /// <param name="missionConfigFilePath">Path must be like this 'MissionConfigs/ChapterX' and path must after 'Resources' folder.</param>
        /// <param name="missionFileName"></param>
        public void Construct(MissionManager missionManager, string missionFileName)
        {
            _missionManager = missionManager;
            _missionFileName = missionFileName;
        }

        public void MissionClicked()
        {
            _missionManager.MissionPaperClicked(_missionFileName);
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}