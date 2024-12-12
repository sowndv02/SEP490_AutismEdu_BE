import { Box, Button, FormControl, InputAdornment, InputLabel, MenuItem, Pagination, Select, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import services from '~/plugins/services'
import LoadingComponent from '~/components/LoadingComponent';
import { Avatar, IconButton, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import VisibilityIcon from '@mui/icons-material/Visibility';
import SearchIcon from '@mui/icons-material/Search';
import { useNavigate } from 'react-router-dom';
import emptyBook from '~/assets/images/icon/emptybook.gif'

function ParentTutorManagement() {
    const nav = useNavigate();
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [filters, setFilters] = useState({
        searchValue: '',
        searchType: 'all',
    });

    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 10,
        total: 10,
    });


    const handleChangeFilters = (key) => (e) => {
        setFilters((prev) => ({ ...prev, [key]: e.target.value }));
        setPagination({
            ...pagination,
            pageNumber: 1,
        });
    };

    useEffect(() => {
        getUsers();
    }, [filters, pagination?.pageNumber]);

    const getUsers = async () => {
        try {
            setLoading(true);
            await services.UserManagementAPI.getUsers((res) => {
                if (res?.result) {
                    setUsers(res.result);
                    setPagination(res.pagination);
                }

            }, (error) => {
                console.log(error);
            }, {
                ...filters,
                pageNumber: pagination?.pageNumber
            });
        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const userProfile = (id, role) => {
        if (id && ['Tutor', 'Parent'].includes(role)) {
            const isTutor = role === 'Tutor';
            if (isTutor) {
                nav(`/admin/tutor-profile/${id}`);
            } else {
                nav(`/admin/parent-profile/${id}`);
            }
        } else {
            return;
        }
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

    return (
        <>
            <Box sx={{
                width: "100%", bgcolor: "white", p: "20px",
                borderRadius: "10px",
                boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
                height: users.length >= 5 ? 'auto' : '650px'
            }}>
                <Typography variant='h4' mt={2} mb={5} textAlign={'center'}>Quản lý người dùng</Typography>
                <Stack direction={'row'} alignItems={'center'} gap={2} mb={5}>
                    <Box width={'80%'}>
                        <TextField
                            fullWidth
                            size='small'
                            label="Tìm kiếm"
                            value={filters.searchValue}
                            onChange={handleChangeFilters('searchValue')}
                            sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                            InputProps={{
                                endAdornment: (
                                    <InputAdornment position="end">
                                        <SearchIcon />
                                    </InputAdornment>
                                ),
                            }}
                        />
                    </Box>
                    <Box width={"20%"}>
                        <FormControl fullWidth size='small'>
                            <InputLabel id="sort-select-label">Vai trò</InputLabel>
                            <Select
                                labelId="searchType-select-label"
                                value={filters.searchType}
                                label="Thứ tự"
                                onChange={handleChangeFilters('searchType')}
                                sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                            >
                                <MenuItem value="all">Tất cả</MenuItem>
                                <MenuItem value="Parent">Parent</MenuItem>
                                <MenuItem value="Tutor">Tutor</MenuItem>
                            </Select>
                        </FormControl>
                    </Box>

                </Stack>
                {
                    users.length !== 0 ?
                        <>
                            <TableContainer component={Paper} sx={{ mt: 3, boxShadow: 3, borderRadius: 2 }}>
                                <Table>
                                    <TableHead>
                                        <TableRow>
                                            <TableCell sx={{ fontWeight: 600 }}>STT</TableCell>
                                            <TableCell sx={{ fontWeight: 600 }}>Người dùng</TableCell>
                                            <TableCell sx={{ fontWeight: 600 }}>Vai trò</TableCell>
                                            <TableCell sx={{ fontWeight: 600 }}>Trạng thái</TableCell>
                                            <TableCell sx={{ fontWeight: 600 }}>Hành động</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {
                                            users?.map((user, index) => {
                                                return (
                                                    <TableRow key={user?.id} hover>
                                                        <TableCell>{index + 1}</TableCell>
                                                        <TableCell>
                                                            <Box sx={{ display: "flex", gap: 1 }}>
                                                                <Avatar alt="Remy Sharp" src={user?.imageUrl} />
                                                                <Box>
                                                                    <Typography sx={{ fontWeight: "bold" }}>{user?.fullName}</Typography>
                                                                    <Typography sx={{ fontSize: "12px" }}>{user?.email}</Typography>
                                                                </Box>
                                                            </Box>
                                                        </TableCell>
                                                        <TableCell>
                                                            {user?.role}
                                                        </TableCell>
                                                        <TableCell>
                                                            <span
                                                                style={{
                                                                    backgroundColor: user.isLockedOut ? '#f8d7da' : '#d4edda',
                                                                    color: user.isLockedOut ? 'red' : 'green',
                                                                    padding: '4px 8px',
                                                                    borderRadius: '8px',
                                                                    fontWeight: 'bold',
                                                                    display: 'inline-block',
                                                                }}
                                                            >
                                                                {user?.isLockedOut ? "Khoá" : "Hoạt động"}
                                                            </span>
                                                        </TableCell>
                                                        <TableCell>
                                                            <IconButton onClick={() => userProfile(user?.id, user?.role)}>
                                                                <VisibilityIcon color='primary' />
                                                            </IconButton>
                                                        </TableCell>
                                                    </TableRow>
                                                )
                                            })
                                        }
                                    </TableBody>
                                    <LoadingComponent open={loading} setLoading={setLoading} />
                                </Table>

                            </TableContainer >
                            <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                                <Pagination
                                    count={totalPages}
                                    page={pagination.pageNumber}
                                    onChange={handlePageChange}
                                    color="primary"
                                />
                            </Stack>
                        </> : <Box sx={{ textAlign: "center" }}>
                            <img src={emptyBook} style={{ height: "200px" }} />
                            <Typography>Hiện tại không có người dùng nào!</Typography>
                        </Box>
                }
            </Box>
            <LoadingComponent open={loading} setLoading={setLoading} />
        </>
    )
}

export default ParentTutorManagement
