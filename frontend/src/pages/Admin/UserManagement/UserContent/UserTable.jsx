import { Avatar, Box, IconButton, Pagination, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material'
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import LockPersonIcon from '@mui/icons-material/LockPerson';
import ActionMenu from './ActionMenu';
import VisibilityIcon from '@mui/icons-material/Visibility';
function UserTable() {

    const arr = [1, 2, 3, 4, 5, 6]
    return (
        <TableContainer component={Paper} sx={{ mt: "20px" }}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>No</TableCell>
                        <TableCell>User</TableCell>
                        <TableCell>Role</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Actions</TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    {
                        arr.map((a) => {
                            return (
                                <TableRow key={a}>
                                    <TableCell>{a}</TableCell>
                                    <TableCell>
                                        <Box sx={{ display: "flex", gap: 1 }}>
                                            <Avatar alt="Remy Sharp" src="https://scontent.fhan19-1.fna.fbcdn.net/v/t39.30808-1/268142468_3035907700072578_4829229204736514171_n.jpg?stp=dst-jpg_p200x200&_nc_cat=100&ccb=1-7&_nc_sid=0ecb9b&_nc_eui2=AeFe_w7HSGpqFDepgviEP4pyq9KSuRzAWe6r0pK5HMBZ7pEuCwmHx3H-gP4TXxRF640CJIZj8zT62i8cDsbhFZrr&_nc_ohc=WJypldhpSngQ7kNvgErul0X&_nc_ht=scontent.fhan19-1.fna&oh=00_AYAXYXl0i8-GvgyLRWATXg3YJjpAKiDfJvvb5WG7g12V5w&oe=66BF9C45" />
                                            <Box>
                                                <Typography sx={{ fontWeight: "bold" }}>Khai dao</Typography>
                                                <Typography sx={{ fontSize: "12px" }}>daoquangkhai2002@gmail.com</Typography>
                                            </Box>
                                        </Box>
                                    </TableCell>
                                    <TableCell>
                                        <Box sx={{ display: "flex", alignItems: "center", gap: 1, fontSize: "16px" }}>
                                            <AdminPanelSettingsIcon sx={{ color: "#ff4d49" }} />
                                            Admin
                                        </Box>
                                    </TableCell>
                                    <TableCell>True</TableCell>
                                    <TableCell>
                                        <Box sx={{ display: "flex", alignItems: "center" }}>
                                            <IconButton aria-label="delete">
                                                <LockPersonIcon />
                                            </IconButton>
                                            <IconButton>
                                                <VisibilityIcon />
                                            </IconButton>
                                            <ActionMenu />
                                        </Box>
                                    </TableCell>
                                </TableRow>
                            )
                        })
                    }
                </TableBody>
            </Table>
            <Box sx={{ p: "10px", display: "flex", justifyContent: "space-between" }}>
                <Typography>Showing 1 to 10 of 47 enteries</Typography>
                <Pagination count={10} color="primary" />
            </Box>
        </TableContainer >
    )
}

export default UserTable
