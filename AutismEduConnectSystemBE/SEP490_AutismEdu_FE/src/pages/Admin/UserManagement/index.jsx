import { Avatar, Box, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
import ConfirmLockDialog from './UserContent/ConfirmLockDialog';
import UserCreation from './UserProfileModal/UserCreation';
function UserManagement() {
    const [users, setUsers] = useState(null);
    const [pagination, setPagination] = useState(null);
    const [loading, setLoading] = useState(false);
    const [searchValue, setSearchValue] = useState("");
    const [currentPage, setCurrentPage] = useState(1);
    useEffect(() => {
        getListAccounts();
    }, [])
    useEffect(() => {
        if (searchValue.trim() !== "") {
            const handler = setTimeout(() => {
                getListAccounts();
            }, 2000)
            return () => {
                clearTimeout(handler)
            }
        }
    }, [searchValue])
    const handleSearch = (e) => {
        setSearchValue(e.target.value);
    }
    const getListAccounts = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.getUsers((res) => {
                setUsers(res.result);
                res.pagination.currentSize = res.result.length
                setPagination(res.pagination);
            }, (err) => {
                console.log(err);
            }, {
                searchType: "all",
                pageNumber: currentPage
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false)
        }
    }
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
                enqueueSnackbar("Mở khoá tài khoản thành công!", { variant: "success" });
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" });
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
                enqueueSnackbar("Khoá tài khoản thành công!", { variant: "success" });
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" });
            })
        }
        setLoading(false);
    }
    return (
        <Box sx={{
            p: 2,
            position: "relative"
        }}>
            <Box sx={{
                width: "100%", bgcolor: "white", p: "20px",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
            }}>
                <Typography variant='h4'>Quản lý nhân viên</Typography>
                <Box sx={{
                    width: "100%",
                    display: "flex",
                    justifyContent: "space-between",
                    gap: 2,
                    mt: 4
                }}>
                    <TextField id="outlined-basic" label="Tìm tài khoản" variant="outlined"
                        onChange={handleSearch}
                        sx={{
                            padding: "0",
                            width: "300px"
                        }} size='small' />
                    <UserCreation setUsers={setUsers} currentPage={currentPage} />
                </Box>
                <Box>
                    <TableContainer component={Paper} sx={{ mt: "20px" }}>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>STT</TableCell>
                                    <TableCell>Người dùng</TableCell>
                                    <TableCell>Số điện thoại</TableCell>
                                    <TableCell>Vai trò</TableCell>
                                    <TableCell>Trạng thái</TableCell>
                                    <TableCell align='center'>Hành động</TableCell>
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
                                                        <Avatar alt="Remy Sharp" src={user.imageUrl} />
                                                        <Box>
                                                            <Typography sx={{ fontWeight: "bold" }}>{user.fullName}</Typography>
                                                            <Typography sx={{ fontSize: "12px" }}>{user.email}</Typography>
                                                        </Box>
                                                    </Box>
                                                </TableCell>
                                                <TableCell>
                                                    {user.phoneNumber}
                                                </TableCell>
                                                <TableCell>
                                                    {user.role}
                                                </TableCell>
                                                <TableCell sx={{ color: user.isLockedOut ? "red" : "green" }}>
                                                    {user.isLockedOut ? "Đã bị khoá" : "Đang hoạt động"}
                                                </TableCell>
                                                <TableCell align='center'>
                                                    <ConfirmLockDialog isLock={user.isLockedOut} name={user.fullName} id={user.id} handleChangeUserStatus={handleChangeUserStatus} />
                                                </TableCell>
                                            </TableRow>
                                        )
                                    })
                                }
                            </TableBody>
                            <LoadingComponent open={loading} setLoading={setLoading} />
                        </Table>
                        <TablePagging pagination={pagination} setPagination={setPagination} setCurrentPage={setCurrentPage} />
                    </TableContainer >
                </Box>
            </Box>
            <LoadingComponent open={loading} setLoading={setLoading} />
        </Box>
    )
}

export default UserManagement
