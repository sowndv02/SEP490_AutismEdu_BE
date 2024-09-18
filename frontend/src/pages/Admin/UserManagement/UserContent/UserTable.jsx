import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import LockPersonIcon from '@mui/icons-material/LockPerson';
import PersonIcon from '@mui/icons-material/Person';
import VisibilityIcon from '@mui/icons-material/Visibility';
import { Avatar, Box, IconButton, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
import ActionMenu from './ActionMenu';
import UserProfileModal from '../UserProfileModal';
function UserTable({ users, setPagination, setUsers, pagination }) {
    const handleChangeUserStatus = (id, status) => {
        if (status) {
            services.UserManagementAPI.unLockUsers(id, (res) => {
                const updatedUser = users.map((u) => {
                    if (u.id !== id) return u;
                    else {
                        u.isLockedOut = false;
                        return u;
                    }
                })
                setUsers(updatedUser);
                enqueueSnackbar("Unlock user successfully!", { variant: "success" });
            }, (err) => {
                enqueueSnackbar("Unlock user failed!", { variant: "error" });
            })
        } else {
            services.UserManagementAPI.lockUsers(id, (res) => {
                const updatedUser = users.map((u) => {
                    if (u.id !== id) return u;
                    else {
                        u.isLockedOut = true;
                        return u;
                    }
                })
                setUsers(updatedUser);
                enqueueSnackbar("Lock user successfully!", { variant: "success" });
            }, (err) => {
                console.log(err);
                enqueueSnackbar("Lock user failed!", { variant: "error" });
            })
        }
    }
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
                        users?.map((user, index) => {
                            return (
                                <TableRow key={user.id}>
                                    <TableCell>{index + 1}</TableCell>
                                    <TableCell>
                                        <Box sx={{ display: "flex", gap: 1 }}>
                                            <Avatar alt="Remy Sharp" src={user.imageLocalUrl} />
                                            <Box>
                                                <Typography sx={{ fontWeight: "bold" }}>{user.fullName}</Typography>
                                                <Typography sx={{ fontSize: "12px" }}>{user.email}</Typography>
                                            </Box>
                                        </Box>
                                    </TableCell>
                                    <TableCell>
                                        {user.role.join(", ")}
                                    </TableCell>
                                    <TableCell>{user.isLockedOut ? "True" : "False"}</TableCell>
                                    <TableCell>
                                        <Box sx={{ display: "flex", alignItems: "center" }}>
                                            <IconButton aria-label="status" onClick={() => handleChangeUserStatus(user.id, user.isLockedOut)}>
                                                {
                                                    user.isLockedOut ? <LockOpenIcon /> : <LockPersonIcon />
                                                }
                                            </IconButton>
                                            <IconButton>
                                                <UserProfileModal currentUser={user} />
                                            </IconButton>
                                            <ActionMenu currentUser={user} />
                                        </Box>
                                    </TableCell>
                                </TableRow>
                            )
                        })
                    }
                </TableBody>
            </Table>
            <TablePagging pagination={pagination} setPaggination={setPagination} />
        </TableContainer >
    )
}

export default UserTable
