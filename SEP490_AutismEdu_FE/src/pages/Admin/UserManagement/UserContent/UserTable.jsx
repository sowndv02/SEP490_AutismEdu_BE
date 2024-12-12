import { Avatar, Box, IconButton, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
import UserProfileModal from '../UserProfileModal';
import ActionMenu from './ActionMenu';
import ConfirmLockDialog from './ConfirmLockDialog';
function UserTable({ users, setPagination, setUsers, pagination, change }) {
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

    useEffect(() => {
        setCurrentPage(1);
    }, [change])
    const handleGetData = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.getUsers((res) => {
                const updatedResult = res.result.map((r) => {
                    let splitedRole = r.role.split(",");
                    r.role = splitedRole;
                    return r;
                })
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
            setLoading(false);
        }
    }
    return (
        <TableContainer component={Paper} sx={{ mt: "20px" }}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>STT</TableCell>
                        <TableCell>Người dùng</TableCell>
                        <TableCell>Vai trò</TableCell>
                        <TableCell>Trạng thái</TableCell>
                        <TableCell>Hành động</TableCell>
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
                                            <ConfirmLockDialog isLock={user.isLockedOut} name={user.fullName} id={user.id} handleChangeUserStatus={handleChangeUserStatus} />
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
                <LoadingComponent open={loading} setLoading={setLoading} />
            </Table>
            <TablePagging pagination={pagination} setPagination={setPagination} setCurrentPage={setCurrentPage} />
        </TableContainer>
    )
}

export default UserTable
