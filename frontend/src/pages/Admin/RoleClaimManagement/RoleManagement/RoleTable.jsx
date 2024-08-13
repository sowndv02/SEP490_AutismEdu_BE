import { Avatar, AvatarGroup, Box, Pagination, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import ListUserModal from '../RoleClaimModal/ListUserModal';
function RoleTable() {
    const arr = [1, 2]
    return (
        <TableContainer component={Paper} sx={{ mt: "20px" }}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>No</TableCell>
                        <TableCell>Role</TableCell>
                        <TableCell>Users</TableCell>
                        <TableCell>Created Date</TableCell>
                        <TableCell>Updated Date</TableCell>
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
                                        Admin
                                    </TableCell>
                                    <TableCell>
                                        <ListUserModal />
                                    </TableCell>
                                    <TableCell>12/4/2024</TableCell>
                                    <TableCell>
                                        12/4/2024
                                    </TableCell>
                                    <TableCell>
                                        Edit
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

export default RoleTable
