@startuml
actor User as user
participant "Giao diện" as ui
participant "UserController" as ctrl
participant "UserService" as service
participant "Database" as db

user -> ui: Truy cập trang hồ sơ
ui -> ctrl: GET /api/users/me
ctrl -> service: GetProfile(userId)
service -> db: SELECT * FROM users WHERE id=?
db --> service: user
service --> ctrl: user profile
ctrl --> ui: 200 OK
ui --> user: Hiển thị thông tin

user -> ui: Sửa tên, avatar
ui -> ctrl: PUT /api/users/me
ctrl -> service: UpdateProfile(userId, data)
service -> db: UPDATE users SET name=?, avatar=? WHERE id=?
db --> service: success
service --> ctrl: updated profile
ctrl --> ui: 200 OK
ui --> user: Thông báo cập nhật thành công
@enduml