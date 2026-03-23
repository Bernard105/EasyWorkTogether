@startuml
left to right direction
actor "User" as user
actor "Email Service" as email

rectangle "Phân hệ Users" {
  usecase "U-105\nKhôi phục mật khẩu" as UC105
  usecase "Gửi email khôi phục" as SendEmail
  UC105 ..> SendEmail : <<include>>
}
user --> UC105
email <-- SendEmail
@enduml