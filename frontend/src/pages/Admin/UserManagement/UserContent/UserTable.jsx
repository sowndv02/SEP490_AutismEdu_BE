<<<<<<< HEAD
import { Avatar, Box, IconButton, Pagination, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material'
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import LockPersonIcon from '@mui/icons-material/LockPerson';
import ActionMenu from './ActionMenu';
import VisibilityIcon from '@mui/icons-material/Visibility';
function UserTable() {

    const arr = [1, 2, 3, 4, 5, 6]
=======
import { Avatar, Box, IconButton, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
import UserProfileModal from '../UserProfileModal';
import ActionMenu from './ActionMenu';
import ConfirmLockDialog from './ConfirmLockDialog';
function UserTable({ users, setPagination, setUsers, pagination }) {
    const [loading, setLoading] = useState(false);
    const [currentPage, setCurrentPage] = useState(1)
    const handleChangeUserStatus = async (id, status) => {
        setLoading(true);
        if (status) {
            await services.UserManagementAPI.unLockUsers(id, (res) => {
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
            await services.UserManagementAPI.lockUsers(id, (res) => {
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
        setLoading(false);
    }

    useEffect(() => {
        handleGetData();
    }, [currentPage]);
    const handleGetData = async () => {
        try {
            setLoading(true);
            console.log(currentPage);
            await services.UserManagementAPI.getUsers((res) => {
                const updatedResult = res.result.map((r) => {
                    let splitedRole = r.role.split(",");
                    r.role = splitedRole;
                    return r;
                })
                console.log(res);
                setUsers(updatedResult);
                res.pagination.currentSize = updatedResult.length
                setPagination(res.pagination);
                setLoading(false);
            }, (err) => {
                setLoading(false);
            }, {
                pageNumber: currentPage || 1
            });
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
<<<<<<< HEAD
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
=======
                        users?.map((user, index) => {
                            return (
                                <TableRow key={user.id}>
                                    <TableCell>{(currentPage - 1) * 10 + index + 1}</TableCell>
                                    <TableCell>
                                        <Box sx={{ display: "flex", gap: 1 }}>
                                            <Avatar alt="Remy Sharp" src={user.imageLocalUrl} />
                                            <Box>
                                                <Typography sx={{ fontWeight: "bold" }}>{user.fullName}</Typography>
                                                <Typography sx={{ fontSize: "12px" }}>{user.email}</Typography>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                                            </Box>
                                        </Box>
                                    </TableCell>
                                    <TableCell>
<<<<<<< HEAD
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
=======
                                        {user.role.join(", ")}
                                    </TableCell>
                                    <TableCell>{user.isLockedOut ? "True" : "False"}</TableCell>
                                    <TableCell>
                                        <Box sx={{ display: "flex", alignItems: "center" }}>
                                            <ConfirmLockDialog isLock={user.isLockedOut} name={user.fullName} id={user.id} handleChangeUserStatus={handleChangeUserStatus} />
                                            <IconButton>
                                                <UserProfileModal currentUser={user} />
                                            </IconButton>
                                            <ActionMenu currentUser={user} />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                                        </Box>
                                    </TableCell>
                                </TableRow>
                            )
                        })
                    }
                </TableBody>
<<<<<<< HEAD
            </Table>
            <Box sx={{ p: "10px", display: "flex", justifyContent: "space-between" }}>
                <Typography>Showing 1 to 10 of 47 enteries</Typography>
                <Pagination count={10} color="primary" />
            </Box>
=======
                <LoadingComponent open={loading} setLoading={setLoading} />
            </Table>
            <TablePagging pagination={pagination} setPagination={setPagination} setCurrentPage={setCurrentPage} />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
        </TableContainer >
    )
}

export default UserTable
