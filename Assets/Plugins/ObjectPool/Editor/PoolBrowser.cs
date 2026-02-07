using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Jackey.ObjectPool.Editor {
	internal class PoolBrowser : EditorWindow {
		private PoolCategory m_activePoolCategory = PoolCategory.None;
		private IPool m_inspectedPool;

		private VisualElement m_categoryRoot;
		private readonly Color m_activeCategoryColor = new(0.345098f, 0.345098f, 0.345098f);
		private readonly Color m_inactiveCategoryColor = new(0.1882353f, 0.1882353f, 0.1882353f);

		private readonly List<IPool> m_poolList = new();
		private ListView m_poolListView;

		private readonly List<object> m_freeObjectsList = new();
		private ListView m_freeObjectsListView;
		private Label m_freeObjectsListCountLabel;
		private readonly List<object> m_activeObjectsList = new();
		private ListView m_activeObjectsListView;
		private Label m_activeObjectsCountLabel;

		private Texture2D m_gameObjectIcon;
		private Texture2D m_pocoIcon;

		private Texture2D m_freeIcon;
		private Texture2D m_activeIcon;

		[MenuItem("Tools/Jackey/Object Pool/Pool Browser", false, 1000)]
		private static void ShowWindow() {
			PoolBrowser window = GetWindow<PoolBrowser>();
			window.Show();
		}

		private void OnEnable() {
			m_gameObjectIcon = (Texture2D)EditorGUIUtility.IconContent("d_Prefab Icon").image;
			m_pocoIcon = (Texture2D)EditorGUIUtility.IconContent("cs Script Icon").image;
			m_freeIcon = (Texture2D)EditorGUIUtility.IconContent("GameObject Icon").image;
			m_activeIcon = (Texture2D)EditorGUIUtility.IconContent("GameObject On Icon").image;
		}

		private void OnDisable() {
			ObjectPool.PoolCreated -= OnPoolCreated;
			ObjectPool.PoolReset -= OnPoolReset;
			ObjectPool.PoolRemoved -= OnPoolRemoved;
			ObjectPool.Cleared -= OnClear;
			ObjectPool.AnyObjectSetup -= OnAnyObjectSetup;
			ObjectPool.AnyObjectReturned -= OnAnyObjectReturned;
		}

		#region GUI

		private void CreateGUI() {
			titleContent.text = "Pool Browser";

			VisualElement splitView = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					flexGrow = 1f,
				},
			};

			VisualElement poolCategoryView = new VisualElement() {
				style = {
					height = new Length(100f, LengthUnit.Percent),
					width = new Length(33f, LengthUnit.Percent),
					borderRightWidth = 1f,
					borderRightColor = new Color(0.3490196f, 0.3490196f, 0.3490196f),
				},
			};
			poolCategoryView.Add(m_categoryRoot = CreatePoolCategoryGUI());
			poolCategoryView.Add(m_poolListView = CreatePoolListGUI());
			splitView.Add(poolCategoryView);

			VisualElement objectListsView = CreateObjectListsGUI();
			splitView.Add(objectListsView);

			rootVisualElement.Add(splitView);

			ObjectPool.PoolCreated += OnPoolCreated;
			ObjectPool.PoolReset += OnPoolReset;
			ObjectPool.PoolRemoved += OnPoolRemoved;
			ObjectPool.Cleared += OnClear;
			ObjectPool.AnyObjectSetup += OnAnyObjectSetup;
			ObjectPool.AnyObjectReturned += OnAnyObjectReturned;

			ShowPoolCategory(PoolCategory.GameObject);
			RefreshEmptyListLabels();
		}

		private VisualElement CreatePoolCategoryGUI() {
			VisualElement root = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					height = 25f,
				},
			};

			Button gameObjectTabButton = CreatePoolCategoryButtonGUI(m_gameObjectIcon, () => ShowPoolCategory(PoolCategory.GameObject));
			root.Add(gameObjectTabButton);

			Button pocoTabButton = CreatePoolCategoryButtonGUI(m_pocoIcon, () => ShowPoolCategory(PoolCategory.Poco));
			root.Add(pocoTabButton);

			return root;
		}

		private Button CreatePoolCategoryButtonGUI(Texture2D icon, Action clickEvent) {
			Button button = new Button(clickEvent) {
				style = {
					flexDirection = FlexDirection.Row,
					flexGrow = 1f,
					alignItems = Align.Center,
					justifyContent = Justify.Center,
					backgroundColor = m_inactiveCategoryColor,
				},
			};
			button.Add(new Image() {
				image = icon,
			});

			Image iconElement = button.Q<Image>();
			iconElement.style.width = 19f;
			iconElement.style.marginRight = 5f;

			return button;
		}

		private ListView CreatePoolListGUI() {
			ListView listView = new ListView {
				itemsSource = m_poolList,
				makeItem = MakePoolListItem,
				bindItem = BindPoolListItem,
				selectionType = SelectionType.Single,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
				showBoundCollectionSize = true,
				fixedItemHeight = 35f,
				style = {
					flexGrow = 1f,
				},
			};
			listView.selectionChanged += _ => {
				if (m_poolListView.selectedIndex != -1)
					ShowPoolObjects(m_poolList[m_poolListView.selectedIndex]);
			};

			return listView;
		}

		private VisualElement MakePoolListItem() {
			VisualElement root = new VisualElement();

			Label typeLabel = new Label() {
				name = "Header",
				style = { unityTextAlign = TextAnchor.MiddleCenter },
			};
			root.Add(typeLabel);

			VisualElement statsRoot = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.SpaceAround,
				},
			};

			statsRoot.Add(CreatePoolCountsGUI("FreeCount", m_freeIcon));
			statsRoot.Add(CreatePoolCountsGUI("ActiveCount", m_activeIcon));

			root.Add(statsRoot);

			return root;
		}

		private VisualElement CreatePoolCountsGUI(string elementName, Texture icon) {
			VisualElement countRoot = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.Center,
				},
			};

			countRoot.Add(new Image() {
				image = icon,
				style = { width = 15f },
			});
			countRoot.Add(new Label() { name = elementName });

			return countRoot;
		}

		private void BindPoolListItem(VisualElement element, int index) {
			IPool pool = m_poolList[index];

			Type poolType = pool.GetType();
			Type poolGeneric = poolType.GetGenericArguments()[0];

			string headerText;
			if (pool is ObjectPool.GameObjectPool) {
				Object original = (Object)poolType.GetProperty(nameof(ObjectPool.GameObjectPool<Object>.Original))!.GetValue(pool);
				headerText = $"{(original ? original.name : "<color=yellow>Unknown</color>")} ({poolGeneric.Name})";
			}
			else {
				headerText = poolGeneric.Name;
			}

			element.Q<Label>("Header").text = headerText;
			element.Q<Label>("FreeCount").text = pool.FreeCount.ToString();
			element.Q<Label>("ActiveCount").text = pool.ActiveCount.ToString();
		}

		private VisualElement CreateObjectListsGUI() {
			VisualElement objectsView = new VisualElement() {
				style = {
					flexGrow = 1f,
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.SpaceAround,
				},
			};

			VisualElement freeObjectsView = CreateObjectListGUI("Free", m_freeObjectsList);
			m_freeObjectsListView = freeObjectsView.Q<ListView>();
			m_freeObjectsListCountLabel = freeObjectsView.Q<Label>("ObjectCount");
			objectsView.Add(freeObjectsView);

			VisualElement activeObjectsView = CreateObjectListGUI("Active", m_activeObjectsList);
			m_activeObjectsListView = activeObjectsView.Q<ListView>();
			m_activeObjectsCountLabel = activeObjectsView.Q<Label>("ObjectCount");
			objectsView.Add(activeObjectsView);

			return objectsView;
		}

		private VisualElement CreateObjectListGUI(string headerText, IList itemsSource) {
			VisualElement root = new VisualElement() {
				style = {
					width = new Length(50f, LengthUnit.Percent),
				},
			};

			VisualElement header = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					justifyContent = Justify.Center,
					minHeight = 25f,
				},
			};
			header.Add(new Label(headerText) {
				style = {
					unityTextAlign = TextAnchor.MiddleCenter,
					unityFontStyleAndWeight = FontStyle.Bold,
				},
			});
			header.Add(new Label("(0)") {
				name = "ObjectCount",
				style = {
					unityTextAlign = TextAnchor.MiddleCenter,
					unityFontStyleAndWeight = FontStyle.Bold,
				},
			});
			root.Add(header);

			ListView objectListView = new ListView() {
				userData = itemsSource,
				itemsSource = itemsSource,
				makeItem = MakeObjectListItem,
				bindItem = BindObjectListItem,
				showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
			};
			objectListView.selectionChanged += _ => EditorGUIUtility.PingObject(itemsSource[objectListView.selectedIndex] as Object);

			root.Add(objectListView);

			return root;
		}

		private VisualElement MakeObjectListItem() {
			VisualElement root = new VisualElement() {
				style = {
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					justifyContent = Justify.Center,
				},
			};

			root.Add(new Image() {
				style = {
					width = 16f,
					height = 16f,
				},
			});
			root.Add(new Label() {
				style = { unityTextAlign = TextAnchor.MiddleCenter },
			});

			return root;
		}

		private void BindObjectListItem(VisualElement element, int index) {
			Image iconElement = element.Q<Image>();
			Label labelElement = element.Q<Label>();

			switch (m_inspectedPool) {
				case ObjectPool.GameObjectPool:
					iconElement.image = m_gameObjectIcon;

					IList itemsSource = (IList)element.FindAncestorUserData();
					Object item = (Object)itemsSource[index];
					labelElement.text = item switch {
						GameObject gameObject => gameObject.name,
						Component component => component.name,
						_ => throw new ArgumentOutOfRangeException(),
					};

					break;
				case ObjectPool.PocoPool:
					iconElement.image = m_pocoIcon;
					labelElement.text = "Instance";

					break;
			}
		}

		#endregion

		private void ShowPoolCategory(PoolCategory category) {
			m_categoryRoot[Mathf.Max(0, (int)m_activePoolCategory - 1)].style.backgroundColor = m_inactiveCategoryColor;

			m_freeObjectsList.Clear();
			m_freeObjectsListView.RefreshItems();

			m_activeObjectsList.Clear();
			m_activeObjectsListView.RefreshItems();

			m_poolListView.ClearSelection();

			switch (category) {
				case PoolCategory.GameObject:
					ShowGameObjectPools();
					break;
				case PoolCategory.Poco:
					ShowPocoPools();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(category), category, null);
			}

			RefreshObjectCountLabels();
			m_categoryRoot[(int)category - 1].style.backgroundColor = m_activeCategoryColor;
		}

		private void ShowGameObjectPools() {
			m_poolList.Clear();

			foreach (IPool pool in ObjectPool.s_gameObjectPools.Values) {
				m_poolList.Add(pool);
			}

			m_poolListView.RefreshItems();
			m_activePoolCategory = PoolCategory.GameObject;

			RefreshEmptyListLabels();
		}

		private void ShowPocoPools() {
			m_poolList.Clear();

			foreach (IPool pool in ObjectPool.s_pocoPools.Values) {
				m_poolList.Add(pool);
			}

			m_poolListView.RefreshItems();
			m_activePoolCategory = PoolCategory.Poco;

			RefreshEmptyListLabels();
		}

		private void ShowPoolObjects(IPool pool) {
			Type poolType = pool.GetType();
			m_inspectedPool = pool;

			IList poolObjects = (IList)poolType
				.GetField("m_objects", BindingFlags.Instance | BindingFlags.NonPublic)!
				.GetValue(pool);
			int poolActiveCount = (int)poolType
				.GetField("m_activeCount", BindingFlags.Instance | BindingFlags.NonPublic)!
				.GetValue(pool);

			m_freeObjectsList.Clear();
			m_activeObjectsList.Clear();

			for (int i = 0; i < poolActiveCount; i++)
				m_activeObjectsList.Add(poolObjects[i]);

			for (int i = poolActiveCount; i < poolObjects.Count; i++)
				m_freeObjectsList.Add(poolObjects[i]);

			m_freeObjectsListView.RefreshItems();
			m_activeObjectsListView.RefreshItems();

			RefreshObjectCountLabels();
			RefreshEmptyListLabels();
		}

		private void RefreshObjectCountLabels() {
			m_freeObjectsListCountLabel.text = $"({m_freeObjectsList.Count})";
			m_activeObjectsCountLabel.text = $"({m_activeObjectsList.Count})";
		}

		private void OnPoolCreated(IPool pool) {
			switch (m_activePoolCategory) {
				case PoolCategory.GameObject when pool is ObjectPool.GameObjectPool:
				case PoolCategory.Poco when pool is ObjectPool.PocoPool:
					m_poolList.Add(pool);
					m_poolListView.RefreshItems();
					break;
			}
		}

		private void OnPoolReset(IPool pool) {
			RefreshPoolListItem(pool);

			if (m_inspectedPool != null)
				ShowPoolObjects(pool);
		}

		private void OnPoolRemoved(IPool pool) {
			m_poolList.Remove(pool);
			m_poolListView.RefreshItems();

			if (m_inspectedPool == pool) {
				m_activeObjectsList.Clear();
				m_activeObjectsListView.RefreshItems();

				m_freeObjectsList.Clear();
				m_freeObjectsListView.RefreshItems();
			}

			RefreshEmptyListLabels();
		}

		private void OnClear() {
			m_poolList.Clear();
			m_poolListView.RefreshItems();

			m_freeObjectsList.Clear();
			m_freeObjectsListView.RefreshItems();
			m_activeObjectsList.Clear();
			m_activeObjectsListView.RefreshItems();

			RefreshEmptyListLabels();
		}

		private void OnAnyObjectSetup(IPool pool, object @object) {
			RefreshPoolListItem(pool);

			if (pool != m_inspectedPool)
				return;

			MoveObjectListItem(
				@object,
				m_freeObjectsList, m_freeObjectsListView,
				m_activeObjectsList, m_activeObjectsListView
			);
		}

		private void OnAnyObjectReturned(IPool pool, object @object) {
			RefreshPoolListItem(pool);

			if (pool != m_inspectedPool)
				return;

			MoveObjectListItem(
				@object,
				m_activeObjectsList, m_activeObjectsListView,
				m_freeObjectsList, m_freeObjectsListView
			);
		}

		private void RefreshPoolListItem(IPool pool) {
			int poolIndex = m_poolList.IndexOf(pool);

			if (poolIndex != -1) {
				m_poolListView.RefreshItem(poolIndex);
			}
		}

		private void MoveObjectListItem(object @object, IList fromList, ListView fromView, IList toList, ListView toView) {
			int objectIndex = fromList.IndexOf(@object);

			if (objectIndex != -1) {
				fromList.RemoveAt(objectIndex);
				fromView.RefreshItems();
			}

			toList.Add(@object);
			toView.RefreshItems();

			RefreshObjectCountLabels();
			RefreshEmptyListLabels();
		}

		private void RefreshEmptyListLabels() {
			const string LABEL_CLASS = "unity-list-view__empty-label";

			Label noPoolsLabel = m_poolListView.Query<Label>(null, LABEL_CLASS).First();
			if (noPoolsLabel != null) {
				noPoolsLabel.text = "No Active Pools";
				noPoolsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			}

			Label freeEmptyLabel = m_freeObjectsListView.Query<Label>(null, LABEL_CLASS).First();
			if (freeEmptyLabel != null) {
				freeEmptyLabel.text = "None";
				freeEmptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			}

			Label activeEmptyLabel = m_activeObjectsListView.Query<Label>(null, LABEL_CLASS).First();
			if (activeEmptyLabel != null) {
				activeEmptyLabel.text = "None";
				activeEmptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			}
		}

		private enum PoolCategory {
			None,
			GameObject,
			Poco,
		}
	}
}
