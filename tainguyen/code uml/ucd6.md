@startuml
left to right direction
actor "User" as user

rectangle "Phân hệ Workspace" {
  usecase "W-201\nTạo Workspace" as UC201
}
user --> UC201
@enduml