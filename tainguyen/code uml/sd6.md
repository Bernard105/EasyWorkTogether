@startuml
actor User as user
participant "Giao diện" as ui
participant "WorkspaceController" as ctrl
participant "WorkspaceService" as service
participant "Database" as db

user -> ui: Nhấn "Tạo workspace"
ui -> ctrl: POST /api/workspaces
ctrl -> service: CreateWorkspace(userId, name)
service -> db: INSERT INTO workspaces (name, owner_id) VALUES (?, ?)
db --> service: workspaceId
service -> db: INSERT INTO workspace_members (workspace_id, user_id, role) VALUES (?, ?, 'owner')
db --> service: success
service --> ctrl: workspace object
ctrl --> ui: 201 Created
ui --> user: Chuyển đến trang quản lý workspace
@enduml