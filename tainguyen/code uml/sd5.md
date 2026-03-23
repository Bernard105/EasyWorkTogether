@startuml
actor User as user
participant "Giao diện" as ui
participant "PasswordController" as ctrl
participant "PasswordService" as service
participant "Database" as db
participant "EmailService" as mail

user -> ui: Nhấn "Quên mật khẩu"
ui -> ctrl: POST /api/forgot-password
ctrl -> service: RequestReset(email)
service -> db: SELECT * FROM users WHERE email=?
db --> service: user exists
service -> service: Tạo mã reset (random)
service -> db: Lưu mã vào bảng reset_tokens
db --> service: success
service -> mail: Gửi email chứa mã reset
mail --> service: sent
service --> ctrl: OK
ctrl --> ui: 200 OK
ui --> user: "Email đã được gửi"

user -> ui: Nhập mã reset + mật khẩu mới
ui -> ctrl: POST /api/reset-password
ctrl -> service: ResetPassword(code, newPassword)
service -> db: Kiểm tra mã reset còn hạn
db --> service: valid
service -> service: bcrypt.hash(newPassword)
service -> db: UPDATE users SET password_hash=? WHERE email=?
db --> service: success
service -> db: Xóa / vô hiệu mã reset
service --> ctrl: OK
ctrl --> ui: 200 OK
ui --> user: "Mật khẩu đã được đặt lại"
@enduml