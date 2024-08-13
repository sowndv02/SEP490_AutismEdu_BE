import { Box, Button, TextField, Typography } from '@mui/material'
import UserTable from './UserTable'

function UserContent() {
    return (
        <Box sx={{
            height: (theme) => `calc(100vh - ${theme.myapp.adminHeaderHeight})`,
            width: "100%",
            marginTop: (theme) => theme.myapp.adminHeaderHeight
        }}>
            <Box sx={{
                width: "100%", bgcolor: "white", p: "20px",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
            }}>
                <Typography variant='h6'>Search</Typography>
                <Box sx={{
                    width: "100%",
                    display: "flex",
                    justifyContent: "end",
                    gap: 2
                }}>
                    <TextField id="outlined-basic" label="Search user" variant="outlined"
                        sx={{
                            padding: "0",
                            width: "300px"
                        }} />
                    <Button variant="contained">Add new user</Button>
                </Box>
                <Box>
                    <UserTable />
                </Box>
            </Box>
        </Box>
    )
}

export default UserContent
