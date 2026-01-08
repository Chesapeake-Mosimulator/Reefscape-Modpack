using System;
using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using Games.Reefscape.FieldScripts;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.Drivetrain;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.TestingMod._614
{
    public class NightHawks : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint Arm;
        [SerializeField] private GenericJoint Intake;

        [Header("Testing Variables")]
        [SerializeField] private float rollerTestVelocity;
        [SerializeField] private float hoverTransitionHeightTest;
        [SerializeField] private float hoverTestHeight;

        [Header("Physics Rollers")]
        [SerializeField] private GenericRoller[] canalRollers;
        [SerializeField] private GenericRoller[] EndEffectorRollers;
        [SerializeField] private GenericRoller[] FrontIntakeRollers;
        [SerializeField] private GenericRoller[] BackIntakeRollers;
        private float EEv4PunchVelocity = 6000f;
        private float CanalIntakeVelocity = 4000f;

        [Header("PIDS")]
        [SerializeField] private PidConstants ArmPid;
        [SerializeField] private PidConstants IntakePid;

        [Header("Coral Setpoints")]
        [SerializeField] private NightHawksSetpoint stow;
        [SerializeField] private NightHawksSetpoint hover;
        [SerializeField] private NightHawksSetpoint intakeGroundCoral;
        [SerializeField] private NightHawksSetpoint groundL1;
        [SerializeField] private NightHawksSetpoint l2;
        [SerializeField] private NightHawksSetpoint l3;

        [Header("Algae Setpoints")]
        [SerializeField] private NightHawksSetpoint intakeGroundAlgae;
        [SerializeField] private NightHawksSetpoint l2Punch;
        [SerializeField] private NightHawksSetpoint l3Punch;

        [Header("Intake Components")]
        [SerializeField] private ReefscapeGamePieceIntake endEffectorCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake endEffectorAlgaeIntake;
        [SerializeField] private ReefscapeGamePieceIntake intakeCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake intakeAlgaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private GamePieceState endEffectorCoralStowState;
        [SerializeField] private GamePieceState endEffectorCoralIntakeState;
        [SerializeField] private GamePieceState algaeStowState;
        [SerializeField] private GamePieceState IntakeCoral;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _elevatorTargetHeight;
        private float _armTargetAngle;
        private float _intakeTargetAngle;

        private bool preAligned;
        private ReefscapeAutoAlign align;




        private bool StationMode;
        private bool GroundMode;

        protected override void Start()
        {
            base.Start();

            Arm.SetPid(ArmPid);
            Intake.SetPid(IntakePid);

            _elevatorTargetHeight = 0;
            _armTargetAngle = 0;
            _intakeTargetAngle = 0;


            RobotGamePieceController.SetPreload(endEffectorCoralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[] { endEffectorCoralStowState, endEffectorCoralIntakeState, IntakeCoral };
            _coralController.intakes.Add(endEffectorCoralIntake);
            _coralController.intakes.Add(intakeCoralIntake);

            _algaeController.gamePieceStates = new[] { algaeStowState };
            _algaeController.intakes.Add(intakeAlgaeIntake);
            //_algaeController.intakes.Add(algaePunch);

            StationMode = true;

            align = gameObject.GetComponent<ReefscapeAutoAlign>();
            preAligned = false;
        }

        private void LateUpdate()
        {
            Arm.UpdatePid(ArmPid);
            Intake.UpdatePid(IntakePid);
        }


        private void FixedUpdate()
        {


            AutoAlignOffsets();
            CheckStationMode();

            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            bool hoverPressed = IntakeAction.IsPressed();
            bool hoverJustPressed = hoverPressed && !_hoverButtonLastFrame;

            //_algaeController.SetTargetState(algaeStowState);
            //_coralController.SetTargetState(endEffectorCoralStowState);



            bool isPunchingAlgae = IntakeAction.IsPressed() && (CurrentSetpoint == ReefscapeSetpoints.LowAlgae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae);

            if (isPunchingAlgae)
            {
                foreach (var roller in EndEffectorRollers)
                {
                    roller.ChangeAngularVelocity(EEv4PunchVelocity);
                }
            }

            bool isHovering = IntakeAction.IsPressed() && CurrentSetpoint == ReefscapeSetpoints.Intake;

            if (isHovering)
            {
                foreach (var canalRoller in canalRollers) //need a new canal field bc it interferes with the punch roller and causes the arm to wig out
                {
                    canalRoller.ChangeAngularVelocity(CanalIntakeVelocity);
                }
            }
            else
            {
                foreach (var canalRoller in canalRollers)
                {
                    canalRoller.ChangeAngularVelocity(0);
                }
            }

            bool isGroundIntaking = IntakeAction.IsPressed() && CurrentSetpoint == ReefscapeSetpoints.Intake && StationMode == false;
            if (isGroundIntaking)
            {
                foreach (var intakeRoller in FrontIntakeRollers)
                {
                    intakeRoller.ChangeAngularVelocity(rollerTestVelocity);
                }
                foreach (var backintakeroller in BackIntakeRollers)
                {
                    backintakeroller.ChangeAngularVelocity(-rollerTestVelocity);
                }
            }
            /*else
            {
                foreach (var intakeRoller in IntakeRollers)
                {
                    intakeRoller.ChangeAngularVelocity(0);
                }
            } */
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Intake:

                    if (CurrentRobotMode == ReefscapeRobotMode.Coral && StationMode == true)
                    {
                        if (hoverJustPressed)
                        {
                            StartCoroutine(NightHawksHoverSequence());
                            /*if (hasCoral = true)
                            {
                                StartCoroutine(ReverseHover());
                            }*/
                        }
                        //SetSetpoint(hover);
                        if (!hasCoral)
                        {
                            _coralController.SetTargetState(endEffectorCoralIntakeState);
                            _coralController.RequestIntake(endEffectorCoralIntake, IntakeAction.IsPressed());
                        }
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Coral && StationMode == false)
                    {
                        SetSetpoint(intakeGroundCoral);
                        if (!hasCoral)
                        {
                            _coralController.SetTargetState(IntakeCoral);
                            _coralController.RequestIntake(intakeCoralIntake, IntakeAction.IsPressed());
                        }
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Algae)
                    {
                        SetSetpoint(intakeGroundAlgae);
                        // _algaeController.SetTargetState(algaeStowState);
                        //_algaeController.RequestIntake(intakeAlgaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae);
                    }
                    break;
                case ReefscapeSetpoints.Place:
                    PlacePiece();
                    //StartCoroutine(PlaceSequence());
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(groundL1);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(l2Punch);
                    //_algaeController.RequestIntake(algaePunch, IntakeAction.IsPressed() && !hasAlgae);
                    //_coralController.RequestIntake(endEffectorCoralIntake, false);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(l3Punch);
                    //_algaeController.RequestIntake(algaePunch, IntakeAction.IsPressed() && !hasAlgae);
                    //_coralController.RequestIntake(endEffectorCoralIntake, false);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
            }
            UpdateSetpoints();
            _hoverButtonLastFrame = hoverPressed;

        }
        private float _hoverTransitionHeight = 22;
        private float _armHoverAngle = 160;
        private float _hoverHeight = 20;

        private bool _hoverSequenceRunning = false;
        private bool _hoverButtonLastFrame = false;

        private IEnumerator NightHawksHoverSequence()
        {
            if (_hoverSequenceRunning) yield break;
            _hoverSequenceRunning = true;

            if (IntakeAction.IsPressed() && StationMode)
            {
                _elevatorTargetHeight = _hoverTransitionHeight;
                yield return new WaitForSeconds(0.5f);
                _armTargetAngle = _armHoverAngle;
                yield return new WaitForSeconds(0.75f);
                _elevatorTargetHeight = _hoverHeight;
                yield return new WaitForSeconds(0.5f);
            }
            _hoverSequenceRunning = false;
        }

        private IEnumerator ReverseHover()
        {
            if (_hoverSequenceRunning) yield break;
            _hoverSequenceRunning = true;

            yield return new WaitForSeconds(0.5f);
            _elevatorTargetHeight = _hoverTransitionHeight;
            yield return new WaitForSeconds(1.0f);
            _armTargetAngle = 0;
            yield return new WaitForSeconds(0.75f);
            _elevatorTargetHeight = 0;
            yield return new WaitForSeconds(0.5f);

            _hoverSequenceRunning = false;
        }

        private bool left;
        private void AutoAlignOffsets()
        {
            if (!AutoAlignLeftAction.IsPressed() && !AutoAlignRightAction.IsPressed())
            {
                preAligned = false;
            }
            float leftAlign = -0.75f;
            float rightAlign = -1.25f;
            if (AutoAlignLeftAction.IsPressed())
            {
                left = true;
            }
            else if (AutoAlignRightAction.IsPressed())
            {
                left = false;
            }
            float xOffset = left ? leftAlign : rightAlign;
            align.offset = new Vector3(xOffset, 0, 3.5f);
        }

        private void CheckStationMode()
        {
            // set mode based on intake selection**
            if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
            {
                StationMode = false;
            }
            else
            {
                StationMode = true;
            }

            // set drop type based on mode
            if (StationMode)
            {
                CurrentCoralStationMode.DropType = DropType.Station;
            }
            else
            {
                CurrentCoralStationMode.DropType = DropType.Ground;
            }
        }


        private void PlacePiece()
        {
            if (CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5));
            }
            else
                if (CurrentRobotMode == ReefscapeRobotMode.Coral && StationMode == true)
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 6));
                }
                else
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral && StationMode == false)
                    {
                        _coralController.ReleaseGamePieceWithForce(new Vector3(6, 0, 0));
                        if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
                        {
                            foreach (var Intakeroller in FrontIntakeRollers)
                            {
                                Intakeroller.flipVelocity();
                            }
                            foreach (var backintakeroller in BackIntakeRollers)
                            {
                                backintakeroller.flipVelocity();
                            }
                        }
                    }
        }

        private IEnumerator PlaceSequence()
        {
            PlacePiece();
            yield return new WaitForSeconds(0.25f);
            SetSetpoint(stow);
        }

        private void SetSetpoint(NightHawksSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _armTargetAngle = setpoint.armAngle;
            _intakeTargetAngle = setpoint.intakeAngle;
        }
        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            Arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X);
            Intake.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X);
        }
    }
}