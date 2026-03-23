@startuml
actor "Người mời" as inviter
actor "Người được mời" as invited
participant "Giao diện" as ui
participant "InvitationController" as ctrl
participant "InvitationService" as service
participant "Database" as db
participant "EmailService" as mail

inviter -> ui: Nhập email cần mời
ui -> ctrl: POST /api/workspaces/{id}/invitations
ctrl -> service: CreateInvitation(workspaceId, inviterId, inviteeEmail)
service -> db: Kiểm tra quyền mời (owner/admin)
db --> service: có quyền
service -> db: Kiểm tra email chưa là thành viên
db --> service: chưa
service -> service: Tạo mã mời (code)
service -> db: INSERT INTO workspace_invitations(...)
db --> service: success
service -> mail: Gửi email chứa link mời
mail --> service: sent
service --> ctrl: OK
ctrl --> ui: 201 Created
ui --> inviter: "Đã gửi lời mời"

... (sau đó)
invited -> ui: Nhấp vào link email
ui -> ctrl: POST /api/invitations/accept
ctrl -> service: AcceptInvitation(code, userId)
service -> db: Kiểm tra code hợp lệ, chưa hết hạn
db --> service: invitation
service -> db: INSERT INTO workspace_members (workspace_id, user_id, role) VALUES (?, ?, 'member')
db --> service: success
service -> db: UPDATE workspace_invitations SET status='accepted' WHERE code=?
service --> ctrl: workspace info
ctrl --> ui: 200 OK
ui --> invited: "Đã tham gia workspace"
@enduml