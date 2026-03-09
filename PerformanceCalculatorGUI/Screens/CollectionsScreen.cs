// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty;
using osuTK;
using PerformanceCalculatorGUI.Components;
using PerformanceCalculatorGUI.Components.TextBoxes;
using PerformanceCalculatorGUI.Configuration;
using PerformanceCalculatorGUI.Screens.Collections;
using PerformanceCalculatorGUI.Screens.Collections.Autobalance;

namespace PerformanceCalculatorGUI.Screens
{
    public partial class CollectionsScreen : PerformanceCalculatorScreen
    {
        public override bool ShouldShowConfirmationDialogOnSwitch => false;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        [Resolved]
        private ScoreCache scoreCache { get; set; } = null!;

        [Resolved]
        private SettingsManager configManager { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private DialogOverlay dialogOverlay { get; set; } = null!;

        [Resolved]
        private NotificationDisplay notificationDisplay { get; set; } = null!;

        [Resolved]
        private DifficultyTuningManager<OsuDifficultyConstants> tuningManager { get; set; } = null!;

        private FillFlowContainer collectionList = null!;
        private CreateCollectionButton createCollectionButton = null!;

        private OsuSpriteText collectionNameText = null!;
        private FillFlowContainer collectionContainer = null!;
        private FillFlowContainer<ScoreContainer> scoresList = null!;
        private AddScoreButton addScoreButton = null!;
        private readonly Bindable<CollectionSortCriteria> sorting = new Bindable<CollectionSortCriteria>(CollectionSortCriteria.None);

        private FillFlowContainer autobalanceParametersContainer = null!;
        private OsuSpriteText autobalanceStatusText = null!;
        private RoundedButton autobalanceRunButton = null!;
        private Box autobalanceProgressFill = null!;
        private OsuSpriteText autobalanceTimeText = null!;
        private string autobalanceStage = "Ready";
        private readonly Stopwatch autobalanceStopwatch = new Stopwatch();
        private ScheduledDelegate? autobalanceElapsedUpdate;
        private readonly Bindable<AutobalanceTarget> autobalanceTarget = new Bindable<AutobalanceTarget>(AutobalanceTarget.Total);
        private readonly Dictionary<DifficultyTuningParameter<OsuDifficultyConstants>, AutobalanceParameterState> autobalanceParameterStates
            = new Dictionary<DifficultyTuningParameter<OsuDifficultyConstants>, AutobalanceParameterState>();
        private bool autobalanceRunning;
        private LimitedLabelledNumberBox saIterationsBox = null!;
        private LimitedLabelledNumberBox saRestartsBox = null!;
        private LimitedLabelledNumberBox saSeedBox = null!;
        private AutobalanceRunner autobalanceRunner = null!;

        private VerboseLoadingLayer loadingLayer = null!;

        private readonly Bindable<Collection?> currentCollection = new Bindable<Collection?>();

        private const string collections_directory = "collections";

        public CollectionsScreen()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[] { new Dimension(GridSizeMode.Absolute, 250), new Dimension() },
                    RowDimensions = new[] { new Dimension() },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6.Darken(0.2f)
                                    },
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Collection List",
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new FillFlowContainer
                                            {
                                                Padding = new MarginPadding { Left = 10f, Right = 15.0f, Vertical = 5f },
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                                Spacing = new Vector2(0, 2f),
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Origin = Anchor.TopCentre,
                                                        Anchor = Anchor.TopCentre,
                                                        Height = 20,
                                                        Text = "Collection list"
                                                    },
                                                    collectionList = new FillFlowContainer
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        Direction = FillDirection.Vertical,
                                                    },
                                                    createCollectionButton = new CreateCollectionButton()
                                                }
                                            }
                                        }
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6
                                    },
                                    new OsuScrollContainer(Direction.Vertical)
                                    {
                                        Name = "Scores",
                                        RelativeSizeAxes = Axes.Both,
                                        Child = collectionContainer = new FillFlowContainer
                                        {
                                            Padding = new MarginPadding { Left = 10f, Right = 15.0f, Vertical = 5f },
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 2f),
                                            Alpha = 0,
                                            Children =
                                            [
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[]
                                                    {
                                                        collectionNameText = new OsuSpriteText
                                                        {
                                                            Origin = Anchor.TopLeft,
                                                            Anchor = Anchor.TopLeft,
                                                            Height = 20
                                                        },
                                                        new OverlaySortTabControl<CollectionSortCriteria>
                                                        {
                                                            Anchor = Anchor.CentreRight,
                                                            Origin = Anchor.CentreRight,
                                                            Margin = new MarginPadding { Right = 20 },
                                                            Current = { BindTarget = sorting }
                                                        }
                                                    }
                                                },
                                                createAutobalanceContainer(),
                                                scoresList = new FillFlowContainer<ScoreContainer>
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                },
                                                addScoreButton = new AddScoreButton()
                                            ]
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                loadingLayer = new VerboseLoadingLayer(true)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };
            sorting.ValueChanged += e => { updateSorting(e.NewValue); };

            currentCollection.ValueChanged += loadCollection;
            createCollectionButton.OnSave += onCollectionAdd;
            addScoreButton.OnAdd += onScoreAdd;
            tuningManager.Current.BindValueChanged(_ =>
            {
                if (currentCollection.Value != null)
                    calculateScores();
            });

            autobalanceRunner = new AutobalanceRunner(scoreCache, rulesets, configManager);
            autobalanceTarget.BindValueChanged(_ =>
            {
                if (!autobalanceRunning)
                    updateAutobalanceBaseline();
            });

            createAutobalanceParameterControls();

            loadCollectionList();

            if (RuntimeInfo.IsDesktop)
                HotReloadCallbackReceiver.CompilationFinished += _ => Schedule(calculateScores);
        }

        private Drawable createAutobalanceContainer()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = ExtendedLabelledTextBox.CORNER_RADIUS,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                        Alpha = 0.6f
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 6),
                        Padding = new MarginPadding { Horizontal = 10, Vertical = 8 },
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = "Autobalance",
                                Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                                Margin = new MarginPadding { Bottom = 2 }
                            },
                            new OverlaySortTabControl<AutobalanceTarget>
                            {
                                Title = "Target",
                                Current = { BindTarget = autobalanceTarget }
                            },
                            new OsuSpriteText
                            {
                                Text = "Parameters",
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                Colour = colourProvider.Light2,
                                Margin = new MarginPadding { Top = 6 }
                            },
                            autobalanceParametersContainer = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 4),
                            },
                            new OsuSpriteText
                            {
                                Text = "SA Settings",
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                Colour = colourProvider.Light2,
                                Margin = new MarginPadding { Top = 6 }
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                ColumnDimensions = new[]
                                {
                                    new Dimension(),
                                    new Dimension(),
                                    new Dimension(),
                                },
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        saIterationsBox = new LimitedLabelledNumberBox
                                        {
                                            Label = "Iterations",
                                            PlaceholderText = "5000",
                                            MinValue = 100,
                                            MaxValue = 100000,
                                        },
                                        saRestartsBox = new LimitedLabelledNumberBox
                                        {
                                            Label = "Restarts",
                                            PlaceholderText = "1",
                                            MinValue = 1,
                                            MaxValue = 20,
                                        },
                                        saSeedBox = new LimitedLabelledNumberBox
                                        {
                                            Label = "Seed",
                                            PlaceholderText = "42",
                                            MinValue = 0,
                                            MaxValue = 999999,
                                        },
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    autobalanceRunButton = new RoundedButton
                                    {
                                        Width = 160,
                                        Height = 40,
                                        Text = "Auto-balance",
                                        Action = runAutobalance,
                                        BackgroundColour = colourProvider.Background1
                                    },
                                    autobalanceStatusText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                        Colour = colourProvider.Light2,
                                        Text = "Ready"
                                    }
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 6,
                                Masking = true,
                                CornerRadius = 3,
                                Margin = new MarginPadding { Top = 4 },
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background6.Lighten(0.1f),
                                        Alpha = 0.6f
                                    },
                                    autobalanceProgressFill = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Width = 0,
                                        Height = 1,
                                        Colour = colourProvider.Background1
                                    }
                                }
                            },
                            autobalanceTimeText = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                Colour = colourProvider.Light2,
                                Text = string.Empty
                            }
                        }
                    }
                }
            };
        }

        private void onScoreAdd(long scoreId)
        {
            if (currentCollection.Value!.Scores.Contains(scoreId))
            {
                notificationDisplay.Display(new Notification($"Score {scoreId} already exists"));
                return;
            }

            currentCollection.Value.Scores = [..currentCollection.Value.Scores, scoreId];

            saveCurrentCollection();
        }

        private void onScoreRemove(ExtendedScore score)
        {
            long scoreId = (long)score.SoloScore.ID!;
            currentCollection.Value!.Scores = currentCollection.Value.Scores.Where(x => x != scoreId).ToArray();

            saveCurrentCollection();
        }

        private void loadCollection(ValueChangedEvent<Collection?> obj)
        {
            if (obj.NewValue == null)
            {
                collectionContainer.Hide();
                return;
            }

            collectionNameText.Text = obj.NewValue!.Name;
            collectionContainer.Show();
            resetAutobalanceUi();

            calculateScores();
        }

        private void saveCurrentCollection()
        {
            persistCurrentCollection();
            calculateScores();
        }

        private void persistCurrentCollection()
        {
            if (currentCollection.Value == null)
                return;

            string path = Path.Combine(collections_directory, currentCollection.Value.FileName);

            File.WriteAllText(path, JsonConvert.SerializeObject(currentCollection.Value));
        }

        private void calculateScores()
        {
            if (currentCollection.Value == null)
                return;

            scoresList.Clear();

            loadingLayer.Show();

            Task.Run(async () =>
            {
                foreach (long scoreId in currentCollection.Value.Scores)
                {
                    var score = await scoreCache.GetScore(scoreId).ConfigureAwait(false);
                    if (score == null)
                        continue;

                    var rulesetInstance = rulesets.GetRuleset(score.RulesetID)!.CreateInstance();

                    var working = ProcessorWorkingBeatmap.FromFileOrId(score.BeatmapID.ToString(), cachePath: configManager.GetBindable<string>(Settings.CachePath).Value);

                    Mod[] mods = score.Mods.Select(x => x.ToMod(rulesetInstance)).ToArray();

                    var scoreInfo = score.ToScoreInfo(rulesets, working.BeatmapInfo);

                    var parsedScore = new ProcessorScoreDecoder(working).Parse(scoreInfo);

                    var tunedRuleset = RulesetHelper.CreateRulesetWithTuning(rulesetInstance.RulesetInfo, tuningManager);
                    var difficultyCalculator = tunedRuleset.CreateDifficultyCalculator(working);
                    var difficultyAttributes = difficultyCalculator.Calculate(mods);
                    var performanceCalculator = tunedRuleset.CreatePerformanceCalculator();
                    if (performanceCalculator == null)
                        continue;

                    var perfAttributes = performanceCalculator.Calculate(parsedScore.ScoreInfo, difficultyAttributes);
                    Schedule(() =>
                    {
                        var scoreContainer = new ScoreContainer(
                            new ExtendedScore(score, difficultyAttributes, perfAttributes),
                            currentCollection.Value!.ExpectedPerformance,
                            persistCurrentCollection,
                            autobalanceTarget);
                        scoreContainer.OnDelete += onScoreRemove;

                        scoresList.Add(scoreContainer);
                    });
                }
            }).ContinueWith(t =>
            {
                Logger.Log(t.Exception?.ToString(), level: LogLevel.Error);
                notificationDisplay.Display(new Notification(t.Exception?.Flatten().Message ?? "Failed to calculate collection"));
            }, TaskContinuationOptions.OnlyOnFaulted).ContinueWith(t =>
            {
                Schedule(() =>
                {
                    updateSorting(sorting.Value);
                    loadingLayer.Hide();

                    if (!autobalanceRunning)
                        updateAutobalanceBaseline();
                });
            }, TaskContinuationOptions.None);
        }

        private void onCollectionAdd(string name)
        {
            string fileName = RandomNumberGenerator.GetString(choices: "abcdefghijklmnopqrstuvwxyz0123456789", length: 16) + ".json";

            var collection = new Collection
            {
                Name = name,
                FileName = fileName,
                Scores = []
            };

            string path = Path.Combine(collections_directory, fileName);

            File.WriteAllText(path, JsonConvert.SerializeObject(collection));

            loadCollectionList();
        }

        private void loadCollectionList()
        {
            if (!Directory.Exists(collections_directory))
            {
                Directory.CreateDirectory(collections_directory);

                return; // nothing to load
            }

            collectionList.Clear();

            var collections = new List<Collection>();

            foreach (string collectionFile in Directory.EnumerateFiles(collections_directory))
            {
                var deserializedCollection = JsonConvert.DeserializeObject<Collection>(File.ReadAllText(collectionFile));

                if (deserializedCollection != null)
                {
                    collections.Add(deserializedCollection);
                }
            }

            foreach (var collection in collections.OrderBy(x => x.Name))
            {
                var collectionButton = new CollectionButton(collection, currentCollection);
                collectionList.Add(collectionButton);

                collectionButton.OnDelete += onCollectionDelete;
            }
        }

        private void onCollectionDelete(Collection collection)
        {
            dialogOverlay.Push(new ConfirmDialog("", () =>
            {
                if (collection == currentCollection.Value)
                    currentCollection.Value = null;

                File.Delete(Path.Combine(collections_directory, collection.FileName));

                loadCollectionList();
            })
            {
                HeaderText = DialogStrings.DeletionHeaderText,
                Icon = FontAwesome.Solid.Trash,
                BodyText = collection.Name
            });
        }

        private void updateSorting(CollectionSortCriteria sortCriteria)
        {
            if (!scoresList.Children.Any())
                return;

            if (sortCriteria == CollectionSortCriteria.None)
            {
                for (int i = 0; i < scoresList.Count; i++)
                {
                    scoresList.SetLayoutPosition(scoresList[i], Array.IndexOf(currentCollection.Value!.Scores, scoresList[i].Score.SoloScore.ID));
                }

                return;
            }

            ScoreContainer[] sortedScores;

            switch (sortCriteria)
            {
                case CollectionSortCriteria.Live:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.LivePP).ToArray();
                    break;

                case CollectionSortCriteria.Local:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.PerformanceAttributes?.Total).ToArray();
                    break;

                case CollectionSortCriteria.Difference:
                    sortedScores = scoresList.Children.OrderByDescending(x => x.Score.PerformanceAttributes?.Total - x.Score.LivePP).ToArray();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sortCriteria), sortCriteria, null);
            }

            for (int i = 0; i < sortedScores.Length; i++)
            {
                scoresList.SetLayoutPosition(sortedScores[i], i);
            }
        }

        #region Autobalance

        private void createAutobalanceParameterControls()
        {
            autobalanceParametersContainer.Clear();
            autobalanceParameterStates.Clear();

            var currentConstants = tuningManager.Current.Value;

            foreach (var section in OsuDifficultyTuningParameters.Sections)
            {
                var sectionBindables = new List<BindableBool>();

                // Section toggle checkbox
                var sectionToggle = new BindableBool { Value = section.Parameters.Any(p => p.DefaultEnabled) };
                var sectionCheckbox = new ExtendedOsuCheckbox(nubOnRight: false)
                {
                    RelativeSizeAxes = Axes.X,
                    Padding = new MarginPadding(4),
                    Current = { BindTarget = sectionToggle },
                };

                var collapseIcon = new SpriteIcon
                {
                    Icon = FontAwesome.Solid.ChevronDown,
                    Size = new Vector2(10),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.Centre,
                    Colour = colourProvider.Light1,
                    Margin = new MarginPadding { Left = 6 },
                };

                var sectionFlow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Padding = new MarginPadding { Left = 16 },
                };

                Action toggleCollapse = () =>
                {
                    if (sectionFlow.Alpha > 0)
                    {
                        sectionFlow.Hide();
                        collapseIcon.RotateTo(-90, 200, Easing.OutQuint);
                    }
                    else
                    {
                        sectionFlow.Show();
                        collapseIcon.RotateTo(0, 200, Easing.OutQuint);
                    }
                };

                autobalanceParametersContainer.Add(new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4, 0),
                    Margin = new MarginPadding { Top = 4 },
                    Children = new Drawable[]
                    {
                        new OsuClickableContainer
                        {
                            Size = new Vector2(16, 20),
                            Action = toggleCollapse,
                            Child = collapseIcon,
                        },
                        new OsuClickableContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Action = toggleCollapse,
                            Child = new OsuSpriteText
                            {
                                Text = section.Title,
                                Colour = colourProvider.Light1,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Padding = new MarginPadding { Top = 4, Bottom = 4 },
                            }
                        },
                        new Container
                        {
                            Size = new Vector2(40, 20),
                            Child = sectionCheckbox,
                        },
                    }
                });

                foreach (var parameter in section.Parameters)
                {
                    var state = new AutobalanceParameterState();
                    state.Enabled.Value = parameter.DefaultEnabled;
                    autobalanceParameterStates[parameter] = state;
                    sectionBindables.Add(state.Enabled);

                    double baseVal = parameter.Getter(currentConstants);
                    string minPlaceholder = computeDefaultLowerBound(parameter, baseVal).ToString("G4");
                    string maxPlaceholder = computeDefaultUpperBound(parameter, baseVal).ToString("G4");

                    var minBox = new NullableLabelledFractionalNumberBox
                    {
                        Label = "Min",
                        PlaceholderText = minPlaceholder,
                        MinValue = 0,
                    };
                    minBox.Value.BindTo(state.MinBound);

                    var maxBox = new NullableLabelledFractionalNumberBox
                    {
                        Label = "Max",
                        PlaceholderText = maxPlaceholder,
                        MinValue = 0,
                    };
                    maxBox.Value.BindTo(state.MaxBound);

                    sectionFlow.Add(new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Absolute, 230),
                            new Dimension(),
                            new Dimension(),
                        },
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Child = new ExtendedOsuCheckbox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Padding = new MarginPadding(4),
                                        Current = { BindTarget = state.Enabled },
                                        LabelText = parameter.UiLabel,
                                        TextColour = colourProvider.Light2
                                    }
                                },
                                minBox,
                                maxBox
                            }
                        }
                    });
                }

                autobalanceParametersContainer.Add(sectionFlow);

                // Wire section toggle to set/unset all parameters in this section
                var capturedBindables = sectionBindables.ToArray();
                bool suppressSectionToggle = false;

                sectionToggle.BindValueChanged(e =>
                {
                    if (suppressSectionToggle)
                        return;

                    foreach (var b in capturedBindables)
                        b.Value = e.NewValue;
                });

                // Update section toggle when individual parameters change
                foreach (var b in capturedBindables)
                {
                    b.BindValueChanged(_ =>
                    {
                        suppressSectionToggle = true;
                        sectionToggle.Value = capturedBindables.Any(bb => bb.Value);
                        suppressSectionToggle = false;
                    });
                }
            }
        }

        private static double computeDefaultLowerBound(DifficultyTuningParameter<OsuDifficultyConstants> parameter, double baseVal)
        {
            if (parameter.MaxValue is { })
                return parameter.MinValue;

            return Math.Max(parameter.MinValue, baseVal * 0.33);
        }

        private static double computeDefaultUpperBound(DifficultyTuningParameter<OsuDifficultyConstants> parameter, double baseVal)
        {
            if (parameter.MaxValue is { } maxVal)
                return maxVal;

            return Math.Max(baseVal * 3.0, parameter.MinValue * 3.0);
        }

        private void runAutobalance()
        {
            if (autobalanceRunning)
                return;

            if (currentCollection.Value == null)
            {
                notificationDisplay.Display(new Notification("Select a collection first."));
                return;
            }

            var selectedParameters = autobalanceParameterStates
                                     .Where(kv => kv.Value.Enabled.Value)
                                     .Select(kv =>
                                     {
                                         var param = kv.Key;
                                         var state = kv.Value;

                                         if (state.MinBound.Value != null || state.MaxBound.Value != null)
                                         {
                                             double min = state.MinBound.Value ?? param.MinValue;
                                             double? max = state.MaxBound.Value ?? param.MaxValue;
                                             return param.WithBounds(min, max);
                                         }

                                         return param;
                                     })
                                     .ToArray();

            if (selectedParameters.Length == 0)
            {
                notificationDisplay.Display(new Notification("Select at least one tuning parameter."));
                return;
            }

            setAutobalanceState(true, "Preparing...");

            var collection = currentCollection.Value;
            var target = autobalanceTarget.Value;

            var saConfig = new SAConfig
            {
                Iterations = saIterationsBox.Value.Value > 0 ? saIterationsBox.Value.Value : 5000,
                Restarts = saRestartsBox.Value.Value > 0 ? saRestartsBox.Value.Value : 1,
                Seed = saSeedBox.Value.Value > 0 ? saSeedBox.Value.Value : 42,
            };

            autobalanceRunner.RunOsuAsync(collection, target, selectedParameters, tuningManager.Current.Value, config: saConfig, progress: onAutobalanceProgress)
                             .ContinueWith(handleAutobalanceResult, TaskContinuationOptions.None);
        }

        private void handleAutobalanceResult(Task<AutobalanceResult<OsuDifficultyConstants>> task)
        {
            if (task.Exception != null)
                Logger.Log(task.Exception.ToString(), level: LogLevel.Error);

            Schedule(() =>
            {
                loadingLayer.Hide();

                var result = task.IsFaulted ? AutobalanceResult<OsuDifficultyConstants>.Failure("Autobalance failed.") : task.GetAwaiter().GetResult();

                if (task.IsFaulted || result.IsFailure)
                {
                    string message = task.IsFaulted
                        ? task.Exception?.Flatten().Message ?? "Autobalance failed."
                        : result.ErrorMessage ?? "Autobalance failed.";

                    notificationDisplay.Display(new Notification(message));
                    setAutobalanceState(false, "Failed");
                    return;
                }

                tuningManager.Current.Value = result.Constants!;
                setAutobalanceProgress(1);
                setAutobalanceState(false, $"RMSE {result.Evaluation.Rmse:0.##}pp, \u03c1={result.Evaluation.Spearman:0.###} ({result.SampleCount} scores)");
            });
        }

        private void resetAutobalanceUi()
        {
            autobalanceStage = "Ready";
            autobalanceStatusText.Text = autobalanceStage;
            autobalanceTimeText.Text = string.Empty;
            setAutobalanceProgress(0);
        }

        private void updateAutobalanceBaseline()
        {
            if (currentCollection.Value == null || !scoresList.Children.Any())
            {
                autobalanceStatusText.Text = "Ready";
                return;
            }

            var expectedPerformance = currentCollection.Value.ExpectedPerformance;

            if (expectedPerformance.Count == 0)
            {
                autobalanceStatusText.Text = "Ready";
                return;
            }

            var target = autobalanceTarget.Value;
            var getTargetValue = AutobalanceEvaluator<OsuDifficultyConstants>.GetOsuTargetValueFunc();
            var pairs = new List<(double actual, double expected, double weight)>();

            foreach (var container in scoresList.Children)
            {
                var score = container.Score;

                if (score.SoloScore.RulesetID != 0)
                    continue;

                string key = score.SoloScore.ID.ToString()!;

                if (!expectedPerformance.TryGetValue(key, out var expectedValues))
                    continue;

                if (!AutobalanceDataset.TryGetExpectedValue(expectedValues, target, out double expectedValue))
                    continue;

                double? actualValue = getTargetValue(score.PerformanceAttributes, target);

                if (actualValue == null)
                    continue;

                double weight = expectedValues.Weight ?? 1.0;
                pairs.Add((actualValue.Value, expectedValue, weight));
            }

            if (pairs.Count == 0)
            {
                autobalanceStatusText.Text = "Ready";
                return;
            }

            double weightSum = pairs.Sum(p => p.weight);
            double weightedMse = pairs.Sum(p => p.weight * (p.actual - p.expected) * (p.actual - p.expected)) / weightSum;
            double rmse = Math.Sqrt(weightedMse);

            if (pairs.Count < 2)
            {
                autobalanceStatusText.Text = $"Ready \u2014 RMSE {rmse:0.##}pp (1 score)";
                return;
            }

            double spearman = AutobalanceEvaluator<OsuDifficultyConstants>.ComputeSpearmanCorrelation(
                pairs.Select(p => p.actual).ToArray(),
                pairs.Select(p => p.expected).ToArray(),
                pairs.Count);
            autobalanceStatusText.Text = $"Ready \u2014 RMSE {rmse:0.##}pp, \u03c1={spearman:0.###} ({pairs.Count} scores)";
        }

        private void setAutobalanceProgress(double progress)
        {
            autobalanceProgressFill.Width = (float)Math.Clamp(progress, 0, 1);
        }

        private void updateAutobalanceElapsed()
        {
            if (!autobalanceRunning)
                return;

            autobalanceTimeText.Text = $"Elapsed {formatElapsed(autobalanceStopwatch.Elapsed)}";
        }

        private static string formatElapsed(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return elapsed.ToString(@"h\:mm\:ss");
            if (elapsed.TotalMinutes >= 1)
                return elapsed.ToString(@"m\:ss\.f");
            return $"{elapsed.TotalSeconds:0.0}s";
        }

        private void onAutobalanceProgress(AutobalanceProgress progress)
        {
            Schedule(() =>
            {
                if (!autobalanceRunning)
                    return;

                setAutobalanceProgress(progress.Value);

                if (!string.IsNullOrEmpty(progress.Stage))
                    autobalanceStage = progress.Stage;

                string percent = $"{progress.Value:0%}";

                if (progress.Total.HasValue && progress.Total.Value > 0 && progress.Completed.HasValue)
                    autobalanceStatusText.Text = $"{autobalanceStage} {progress.Completed.Value}/{progress.Total.Value} ({percent})";
                else
                    autobalanceStatusText.Text = $"{autobalanceStage} ({percent})";
            });
        }

        private void setAutobalanceState(bool running, string status)
        {
            autobalanceRunning = running;
            autobalanceRunButton.Enabled.Value = !running;
            autobalanceStage = status;
            autobalanceStatusText.Text = status;

            if (running)
            {
                autobalanceStopwatch.Restart();
                autobalanceTimeText.Text = "Elapsed 0.0s";
                setAutobalanceProgress(0);

                autobalanceElapsedUpdate?.Cancel();
                autobalanceElapsedUpdate = Scheduler.AddDelayed(updateAutobalanceElapsed, 100, true);

                loadingLayer.Show();
            }
            else
            {
                autobalanceElapsedUpdate?.Cancel();
                autobalanceElapsedUpdate = null;

                autobalanceStopwatch.Stop();
                autobalanceTimeText.Text = $"Took {formatElapsed(autobalanceStopwatch.Elapsed)}";
            }
        }

        #endregion

        private class AutobalanceParameterState
        {
            public BindableBool Enabled { get; } = new BindableBool();
            public Bindable<double?> MinBound { get; } = new Bindable<double?>();
            public Bindable<double?> MaxBound { get; } = new Bindable<double?>();
        }
    }
}
