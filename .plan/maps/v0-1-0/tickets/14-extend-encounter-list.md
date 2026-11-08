---
type: task
blocked_by: [09, 12]
undermined_by: []
assets: []
---

# Extend the normal encounter list on Wayfinder runs

## Question

Change `ActModel.GenerateRooms` as [the content decision](./09-content-built-for-shorter-acts.md) describes. Double the weak encounters and make the list twice `GetNumberOfRooms`, only inside that method, drawing from the existing grab bag. The patch reads `EnableWayfinder` itself, so it needs no setup patch. Confirm that read agrees with the read for the first map.
