// /**************************************************************************/
// /*  animation_track_editor.h, animation_track_editor.cpp                  */
// /**************************************************************************/
// /*                         This file is part of:                          */
// /*                             GODOT ENGINE                               */
// /*                        https://godotengine.org                         */
// /**************************************************************************/
// /* Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md). */
// /* Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.                  */
// /*                                                                        */
// /* Permission is hereby granted, free of charge, to any person obtaining  */
// /* a copy of this software and associated documentation files (the        */
// /* "Software"), to deal in the Software without restriction, including    */
// /* without limitation the rights to use, copy, modify, merge, publish,    */
// /* distribute, sublicense, and/or sell copies of the Software, and to     */
// /* permit persons to whom the Software is furnished to do so, subject to  */
// /* the following conditions:                                              */
// /*                                                                        */
// /* The above copyright notice and this permission notice shall be         */
// /* included in all copies or substantial portions of the Software.        */
// /*                                                                        */
// /* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,        */
// /* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF     */
// /* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. */
// /* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY   */
// /* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,   */
// /* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE      */
// /* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                 */
// /**************************************************************************/
// using System.Collections.Generic;
// using Godot;
// using Godot.Bridge;
// using Godot.Collections;

// namespace PCE.Editor;

// [GlobalClass]
// public partial class AnimationTrackEditor : VBoxContainer {
//     private const float FLT_MAX = -1;
//     Animation animation;
//     bool read_only = false;
//     Node root;

//     MenuButton edit;

//     PanelContainer main_panel;
//     HScrollBar hscroll;
//     ScrollContainer scroll;
//     VBoxContainer track_vbox;
//     // AnimationBezierTrackEdit bezier_edit;
//     VBoxContainer timeline_vbox;

//     Label info_message;

//     // AnimationTimelineEdit timeline;
//     // AnimationMarkerEdit marker_edit;
//     HSlider zoom;
//     EditorSpinSlider step;
//     TextureRect zoom_icon;
//     Button snap_keys;
//     Button snap_timeline;
//     Button bezier_edit_icon;
//     OptionButton snap_mode;
//     Button auto_fit;
//     Button auto_fit_bezier;

//     Button imported_anim_warning;
//     void _show_imported_anim_warning() { }

//     Button dummy_player_warning;
//     void _show_dummy_player_warning() { }

//     Button inactive_player_warning;
//     void _show_inactive_player_warning() { }

//     void _snap_mode_changed(int p_mode) { }
//     // Vector<AnimationTrackEdit *> track_edits;
//     // Vector<AnimationTrackEditGroup *> groups;

//     bool animation_changing_awaiting_update = false;
//     void _animation_update() { } // Updated by AnimationTrackEditor(this)
//     int _get_track_selected() { }
//     void _animation_changed() { }
//     void _update_tracks() { }
//     void _redraw_tracks() { }
//     void _redraw_groups() { }
//     void _check_bezier_exist() { }

//     void _name_limit_changed() { }
//     void _timeline_changed(float p_new_pos, bool p_timeline_only) { }
//     void _track_remove_request(int p_track) { }
//     void _animation_track_remove_request(int p_track, Animation p_from_animation) { }
//     void _track_grab_focus(int p_track) { }

//     void _update_scroll(double _) { }
//     void _update_step(double p_new_step) { }
//     void _update_length(double p_new_len) { }
//     void _dropped_track(int p_from_track, int p_to_track) { }

//     void _add_track(int p_type) { }
//     void _new_track_node_selected(NodePath p_path) { }
//     void _new_track_property_selected(string p_name) { }

//     void _update_step_spinbox() { }

//     // PropertySelector *prop_selector;
//     // PropertySelector *method_selector;
//     // SceneTreeDialog *pick_track;
//     int adding_track_type = 0;
//     NodePath adding_track_path;

//     bool keying = false;

//     class InsertData {
//         Animation.TrackType type;
//         NodePath path;
//         int track_idx = 0;
//         float time = FLT_MAX; // Defaults to current timeline position.
//         Variant value;
//         string query;
//         bool advance = false;
//     };

//     Label insert_confirm_text;
//     CheckBox insert_confirm_bezier;
//     CheckBox insert_confirm_reset;
//     ConfirmationDialog insert_confirm;
//     bool insert_queue = false;
//     List<InsertData> insert_data;

//     void _query_insert(InsertData p_id) { }
//     Animation _create_and_get_reset_animation() { }
//     void _confirm_insert_list() { }
//     struct TrackIndices {
//         int normal;
//         int reset;

//         public TrackIndices(Animation p_anim, Animation p_reset_anim) {

//             normal = p_anim is not null ? p_anim.GetTrackCount() : 0;
//             reset = p_reset_anim is not null ? p_reset_anim.GetTrackCount() : 0;
//         }
//     };
//     TrackIndices _confirm_insert(InsertData p_id, TrackIndices p_next_tracks, bool p_reset_wanted, Animation p_reset_anim, bool p_create_beziers) { }
//     void _insert_track(bool p_reset_wanted, bool p_create_beziers) { }

//     void _root_removed() { }

//     PropertyInfo _find_hint_for_track(int p_idx, NodePath r_base_path, Variant r_current_val) { }

//     void _scroll_changed(Vector2 p_val) { }
//     void _v_scroll_changed(float p_val) { }
//     void _h_scroll_changed(float p_val) { }

//     ViewPanner panner;
//     void _pan_callback(Vector2 p_scroll_vec, InputEvent p_event) { }
//     void _zoom_callback(float p_zoom_factor, Vector2 p_origin, InputEvent p_event) { }

//     void _timeline_value_changed(double _) { }

//     float insert_key_from_track_call_ofs = 0f;
//     int insert_key_from_track_call_track = 0;
//     void _insert_key_from_track(float p_ofs, int p_track) { }
//     void _add_method_key(string p_method) { }

//     void _fetch_value_track_options(NodePath p_path, ref Animation.UpdateMode r_update_mode, ref Animation.InterpolationType r_interpolation_type, ref bool r_loop_wrap) { }

//     void _clear_selection_for_anim(Animation p_anim) { }
//     void _select_at_anim(Animation p_anim, int p_track, float p_pos) { }

//     //selection

//     struct SelectedKey {
//         public int track = 0;
//         public int key = 0;

//         public SelectedKey() {
//         }

//         public static bool operator <(SelectedKey lhs, SelectedKey rhs) {
//             return lhs.track == rhs.track ? lhs.key < rhs.key : lhs.track < rhs.track;
//         }

//         public static bool operator >(SelectedKey lhs, SelectedKey rhs) {
//             return lhs.track == rhs.track ? lhs.key > rhs.key : lhs.track > rhs.track;
//         }
//     };

//     struct KeyInfo {
//         float pos = 0;

//         public KeyInfo() {
//         }
//     };

//     SortedDictionary<SelectedKey, KeyInfo> selection;

//     bool moving_selection = false;
//     float moving_selection_offset = 0.0f;
//     void _move_selection_begin() { }
//     void _move_selection(float p_offset) { }
//     void _move_selection_commit() { }
//     void _move_selection_cancel() { }

//     // AnimationTrackKeyEdit *key_edit;
//     // AnimationMultiTrackKeyEdit *multi_key_edit;
//     void _update_key_edit() { }
//     void _clear_key_edit() { }

//     Control box_selection_container;

//     Control box_selection;
//     void _box_selection_draw() { }
//     bool box_selecting = false;
//     Vector2 box_selecting_from;
//     Vector2 box_selecting_to;
//     Rect2 box_select_rect;
//     Vector2 prev_scroll_position;
//     void _scroll_input(InputEvent p_event) { }

// // Vector<Ref<AnimationTrackEditPlugin>> track_edit_plugins;

//     void _toggle_bezier_edit() { }
//     void _cancel_bezier_edit() { }
//     void _bezier_edit(int p_for_track) { }
//     // needs TOOLS_ENABLED
//     // void _bezier_track_set_key_handle_mode(Animation p_anim, int p_track, int p_index, Animation::HandleMode p_mode, Animation::HandleSetMode p_set_mode = Animation::HANDLE_SET_MODE_NONE) { }

//     ////////////// edit menu stuff

//     ConfirmationDialog bake_dialog;
//     CheckBox bake_trs;
//     CheckBox bake_blendshape;
//     CheckBox bake_value;
//     SpinBox bake_fps;

//     ConfirmationDialog optimize_dialog;
//     SpinBox optimize_velocity_error;
//     SpinBox optimize_angular_error;
//     SpinBox optimize_precision_error;

//     ConfirmationDialog cleanup_dialog;
//     CheckBox cleanup_keys_with_trimming_head;
//     CheckBox cleanup_keys_with_trimming_end;
//     CheckBox cleanup_keys;
//     CheckBox cleanup_tracks;
//     CheckBox cleanup_all;

//     ConfirmationDialog scale_dialog;
//     SpinBox scale;

//     ConfirmationDialog ease_dialog;
//     OptionButton transition_selection;
//     OptionButton ease_selection;
//     SpinBox ease_fps;

//     void _select_all_tracks_for_copy() { }

//     void _edit_menu_about_to_popup() { }
//     void _edit_menu_pressed(int p_option) { }
//     int last_menu_track_opt = 0;

//     void _cleanup_animation(Animation p_animation) { }

//     void _anim_duplicate_keys(float p_ofs, bool p_ofs_valid, int p_track) { }

//     void _anim_copy_keys(bool p_cut) { }

//     bool _is_track_compatible(int p_target_track_idx, Variant.Type p_source_value_type, Animation.TrackType p_source_track_type) { }

//     void _anim_paste_keys(float p_ofs, bool p_ofs_valid, int p_track) { }

//     void _view_group_toggle() { }
//     Button view_group;
//     Button selected_filter;

//     void _auto_fit() { }
//     void _auto_fit_bezier() { }

//     void _selection_changed() { }

//     ConfirmationDialog track_copy_dialog;
//     Tree track_copy_select;

//     class TrackClipboard {
//         NodePath full_path;
//         NodePath base_path;
//         Animation.TrackType track_type = Animation.TrackType.Animation;
//         Animation.InterpolationType interp_type = Animation.InterpolationType.CubicAngle;
//         Animation.UpdateMode update_mode = Animation.UpdateMode.Capture;
//         Animation.LoopModeEnum loop_mode = Animation.LoopModeEnum.Pingpong;
//         bool loop_wrap = false;
//         bool enabled = false;
//         bool use_blend = false;

//         class Key {
//             float time = 0;
//             float transition = 0;
//             Variant value;
//         };
//         List<Key> keys = [];
//     };

//     struct KeyClipboard {
//         int top_track;

//         class Key {
//             Animation.TrackType track_type;
//             int track;
//             float time = 0;
//             float transition = 0;
//             Variant value;
//         };
//         List<Key> keys;
//     };

//     List<TrackClipboard> track_clipboard;
//     KeyClipboard key_clipboard;

//     void _set_key_clipboard(int p_top_track, float p_top_time, SortedDictionary<SelectedKey, KeyInfo> p_keymap) { }
//     void _insert_animation_key(NodePath p_path, Variant p_value) { }

// void _pick_track_filter_text_changed(string p_newtext) { }
// void _pick_track_select_recursive(TreeItem p_item, string p_filter, List<Node> p_select_candidates) { }

// double snap_unit;
//     void _update_snap_unit() { }

//     protected:
// 	static void _bind_methods() { }
//     void _notification(int p_what) { }

//     public:
// 	// Public for use with callable_mp.
// 	void _clear_selection(bool p_update = false) { }
//     void _key_selected(int p_key, bool p_single, int p_track) { }
//     void _key_deselected(int p_key, int p_track) { }

//     enum EditActionType {
//         EDIT_COPY_TRACKS,
//         EDIT_COPY_TRACKS_CONFIRM,
//         EDIT_PASTE_TRACKS,
//         EDIT_CUT_KEYS,
//         EDIT_COPY_KEYS,
//         EDIT_PASTE_KEYS,
//         EDIT_SCALE_SELECTION,
//         EDIT_SCALE_FROM_CURSOR,
//         EDIT_SCALE_CONFIRM,
//         EDIT_SET_START_OFFSET,
//         EDIT_SET_END_OFFSET,
//         EDIT_EASE_SELECTION,
//         EDIT_EASE_CONFIRM,
//         EDIT_DUPLICATE_SELECTED_KEYS,
//         EDIT_DUPLICATE_SELECTION,
//         EDIT_DUPLICATE_TRANSPOSED,
//         EDIT_MOVE_FIRST_SELECTED_KEY_TO_CURSOR,
//         EDIT_MOVE_LAST_SELECTED_KEY_TO_CURSOR,
//         EDIT_ADD_RESET_KEY,
//         EDIT_DELETE_SELECTION,
//         EDIT_GOTO_NEXT_STEP,
//         EDIT_GOTO_NEXT_STEP_TIMELINE_ONLY, // Next step without updating animation.
//         EDIT_GOTO_PREV_STEP,
//         EDIT_APPLY_RESET,
//         EDIT_BAKE_ANIMATION,
//         EDIT_BAKE_ANIMATION_CONFIRM,
//         EDIT_OPTIMIZE_ANIMATION,
//         EDIT_OPTIMIZE_ANIMATION_CONFIRM,
//         EDIT_CLEAN_UP_ANIMATION,
//         EDIT_CLEAN_UP_ANIMATION_CONFIRM
//     };

//     // void add_track_edit_plugin(const Ref<AnimationTrackEditPlugin> &p_plugin) { }
// // void remove_track_edit_plugin(const Ref<AnimationTrackEditPlugin> &p_plugin) { }

// void set_animation(Animation p_anim, bool p_read_only) { }
// Animation get_current_animation() { }
//     void set_root(Node p_root) { }
//     Node get_root() { }
//     void update_keying() { }
//     bool has_keying() { }

//     Dictionary get_state() { }
//     void set_state(Dictionary p_state) { }

//     void cleanup() { }

//     void set_anim_pos(float p_pos) { }
//     void insert_node_value_key(Node p_node, string p_property, bool p_only_if_exists = false, bool p_advance = false) { }
//     void insert_value_key(string p_property, bool p_advance) { }
//     void insert_transform_key(Node3D p_node, string p_sub, Animation.TrackType p_type, Variant p_value) { }
//     bool has_track(Node3D p_node, string p_sub, Animation.TrackType p_type) { }
//     void make_insert_queue() { }
//     void commit_insert_queue() { }

//     void show_select_node_warning(bool p_show) { }
//     void show_dummy_player_warning(bool p_show) { }
//     void show_inactive_player_warning(bool p_show) { }

//     bool is_key_selected(int p_track, int p_key) { }
//     bool is_selection_active() { }
//     bool is_key_clipboard_active() { }
//     bool is_moving_selection() { }
//     bool is_snap_timeline_enabled() { }
//     bool is_snap_keys_enabled() { }
//     bool is_bezier_editor_active() { }
//     bool can_add_reset_key() { }
//     float get_moving_selection_offset() { }
//     float snap_time(float p_value, bool p_relative = false) { }
//     bool is_grouping_tracks() { }
//     string[] get_selected_section() { }
//     bool is_marker_selected(StringName p_marker) { }
//     bool is_marker_moving_selection() { }
//     float get_marker_moving_selection_offset() { }

//     /** If `p_from_mouse_event` is `true`, handle Shift key presses for precise snapping. */
//     void goto_prev_step(bool p_from_mouse_event) { }

//     /** If `p_from_mouse_event` is `true`, handle Shift key presses for precise snapping. */
//     void goto_next_step(bool p_from_mouse_event, bool p_timeline_only = false) { }

//     MenuButton get_edit_menu() { }
//     AnimationTrackEditor() { }
//     ~AnimationTrackEditor() { }
// }