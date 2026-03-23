@startuml
left to right direction
actor "User" as user

rectangle "Phân hệ Users" {
  usecase "U-104\nQuản lý hồ sơ người dùng" as UC104
}
user --> UC104
@enduml