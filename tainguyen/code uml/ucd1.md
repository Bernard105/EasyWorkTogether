@startuml
left to right direction
actor "User" as user

rectangle "Phân hệ Users" {
  usecase "U-101\nĐăng ký tài khoản" as UC101
}

user --> UC101
@enduml