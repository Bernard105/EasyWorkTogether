@startuml
actor User as user
participant "Giao diện" as ui
participant "WorkspaceController" as ctrl
participant "WorkspaceService" as service
participant "Database" as db

user -> ui: Chọn workspace cần quản lý
ui -> ctrl: GET /api/workspaces/{id}
ctrl -> service: GetWorkspace(id, userId)
service -> db: Kiểm tra quyền (role IN ('owner','admin'))
db --> service: có quyền
service -> db: SELECT * FROM workspaces WHERE id=?
db --> service: workspace
service --> ctrl: workspace
ctrl --> ui: 200 OK
ui --> user: Hiển thị form cập nhật

user -> ui: Sửa tên, cấu hình
ui -> ctrl: PUT /api/workspaces/{id}
ctrl -> service: UpdateWorkspace(id, data, userId)
service -> db: UPDATE workspaces SET ... WHERE id=?
db --> service: success
service --> ctrl: updated workspace
ctrl --> ui: 200 OK
ui --> user: Thông báo thành công
@enduml