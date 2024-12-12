import { Box, IconButton, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography, Paper, TextField, InputAdornment, MenuItem, FormControl, InputLabel, Select, Pagination, Button } from '@mui/material';
import React, { useEffect, useState } from 'react';
import SearchIcon from '@mui/icons-material/Search';
import VisibilityIcon from '@mui/icons-material/Visibility';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import UpdateRequestDetail from './Modal/UpdateRequestDetail';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import ConfirmAcceptDialog from './Modal/ConfirmAcceptDialog';
import ConfirmRejectDialog from './Modal/ConfirmRejectDialog';
import { enqueueSnackbar } from 'notistack';
import { format } from 'date-fns';
import emptyBook from '~/assets/images/icon/emptybook.gif'

const UpdateRequest = () => {
    const [acceptDialogOpen, setAcceptDialogOpen] = useState(false);
    const [rejectDialogOpen, setRejectDialogOpen] = useState(false);
    const [currentRequestId, setCurrentRequestId] = useState(null);
    const [loading, setLoading] = useState(false);
    const [requestList, setRequestList] = useState([]);

    const [filters, setFilters] = React.useState({
        search: '',
        status: 'all',
        orderBy: 'createdDate',
        sort: 'desc',
    });

    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 5,
        total: 5,
    });

    const [selectedRequest, setSelectedRequest] = useState(null);
    const [dialogOpen, setDialogOpen] = useState(false);

    useEffect(() => {
        handleGetUpdateRequestList();
    }, [filters, pagination.pageNumber]);

    const handleGetUpdateRequestList = async () => {
        try {
            setLoading(true);
            await services.TutorManagementAPI.handleGetTutorUpdateRequest((res) => {
                if (res?.result) {
                    setRequestList(res.result);
                    setPagination(res?.pagination);
                }
            }, (error) => {
                console.log(error);
            }, {
                ...filters,
                pageNumber: pagination.pageNumber
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const handleViewRequest = (id) => {
        const request = requestList.find((req) => req.id === id);
        setSelectedRequest(request);
        setDialogOpen(true);
    };

    const handleCloseDialog = () => {
        setDialogOpen(false);
        setSelectedRequest(null);
    };

    const handleFilterChange = (key) => (event) => {
        setFilters({
            ...filters,
            [key]: event.target.value,
        });
        setPagination({
            ...pagination,
            pageNumber: 1,
        });
    };


    const handleAccept = async (id) => {
        await handleChangeStatus(id, 1, "");
        console.log(`Accepted request ID: ${id}`);
    };

    const handleReject = async (id, reason) => {
        await handleChangeStatus(id, 0, reason);
        console.log(`Rejected request ID: ${id} with reason: ${reason}`);
    };

    const handleChangeStatus = async (id, statusChange, rejectionReason) => {
        try {
            setLoading(true);
            await services.TutorManagementAPI.handleUpdateTutorChangeStatus(id, {
                id,
                statusChange,
                rejectionReason
            }, (res) => {
                if (res?.result) {
                    let newData = requestList.find((r) => r.id === id);
                    newData.requestStatus = res.result.requestStatus;
                    newData.rejectionReason = res.result.rejectionReason;
                    setRequestList(requestList);
                    enqueueSnackbar('Đổi trạng thái thành công!', { variant: 'success' });
                }
            }, (error) => {
                console.log(error);
            });

        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };

    const getStatusLabel = (status) => {
        switch (status) {
            case 1:
                return "Chấp nhận";
            case 2:
                return "Đang chờ";
            case 0:
                return "Từ chối";
            default:
                return "Không xác định";
        }
    };

    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);
    console.log(totalPages);

    return (
        <Box sx={{
            height: (theme) => `calc(90vh - ${theme.myapp.adminHeaderHeight})`,
            p: 0,
        }}>
            <Box
                sx={{
                    width: "100%",
                    bgcolor: "white",
                    p: "30px",
                    borderRadius: "10px",
                    boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
                    display: "flex",
                    flexDirection: "column",
                    gap: 2,
                    height: requestList?.length >= 5 ? 'auto' : '650px',
                    margin: "auto"
                }}
            >
                <Typography variant="h4" sx={{ mb: 3 }}>
                    Quản lý danh sách yêu cầu
                </Typography>
                <Stack direction={'row'} justifyContent={'space-between'} alignItems="center" sx={{ width: "100%", mb: 2 }} spacing={3}>
                    <Box sx={{ flex: 2, mr: 3 }}>
                        <TextField
                            fullWidth
                            size='small'
                            label="Tìm kiếm theo email"
                            value={filters.search}
                            onChange={handleFilterChange('search')}
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

                    <Stack direction={'row'} justifyContent={'flex-end'} spacing={2} sx={{ flex: 1 }}>
                        <Box sx={{ flexBasis: '45%' }}>
                            <FormControl fullWidth size='small'>
                                <InputLabel id="status-select-label">Trạng thái</InputLabel>
                                <Select
                                    labelId="status-select-label"
                                    value={filters.status}
                                    label="Trạng thái"
                                    onChange={handleFilterChange('status')}
                                    sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                                >
                                    <MenuItem value={'all'}>Tất cả</MenuItem>
                                    <MenuItem value={'approve'}>Đã chấp nhận</MenuItem>
                                    <MenuItem value={'pending'}>Đang chờ</MenuItem>
                                    <MenuItem value={'reject'}>Từ chối</MenuItem>
                                </Select>
                            </FormControl>
                        </Box>

                        <Box sx={{ flexBasis: '45%' }}>
                            <FormControl fullWidth size='small'>
                                <InputLabel id="sort-select-label">Thứ tự</InputLabel>
                                <Select
                                    labelId="sort-select-label"
                                    value={filters.sort}
                                    label="Thứ tự"
                                    onChange={handleFilterChange('sort')}
                                    sx={{ backgroundColor: '#fff', borderRadius: '4px' }}
                                >
                                    <MenuItem value="asc">Tăng dần theo ngày</MenuItem>
                                    <MenuItem value="desc">Giảm dần theo ngày</MenuItem>
                                </Select>
                            </FormControl>
                        </Box>
                    </Stack>

                </Stack>

                {requestList.length === 0 ? <Box sx={{ textAlign: "center" }}>
                    <img src={emptyBook} style={{ height: "200px" }} />
                    <Typography>Hiện tại chưa có dữ liệu!</Typography>
                </Box>
                    :
                    <>
                        <TableContainer component={Paper} sx={{ borderRadius: 2, boxShadow: 3 }} hover>
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell sx={{ fontWeight: 'bold' }}>ID</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Email</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Họ và tên</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Trạng thái</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {requestList.map((request, index) => (
                                        <TableRow key={request.id} hover>
                                            <TableCell>{index + 1 + (pagination?.pageNumber - 1) * 5}</TableCell>
                                            <TableCell>{request?.tutor?.email}</TableCell>
                                            <TableCell>{request?.tutor?.fullName}</TableCell>
                                            <TableCell>
                                                <Button
                                                    variant="outlined"
                                                    color={
                                                        request.requestStatus === 1 ? 'success' :
                                                            request.requestStatus === 0 ? 'error' :
                                                                'warning'
                                                    }
                                                    size="small"
                                                    sx={{ borderRadius: 2, textTransform: 'none' }}
                                                >
                                                    {getStatusLabel(request?.requestStatus)}
                                                </Button>


                                            </TableCell>
                                            <TableCell>
                                                {request?.createdDate && format(request.createdDate, 'HH:mm dd-MM-yyyy')}
                                            </TableCell>
                                            <TableCell>
                                                <Stack direction="row" spacing={1}>
                                                    <IconButton color="primary" onClick={() => handleViewRequest(request.id)}>
                                                        <VisibilityIcon />
                                                    </IconButton>
                                                    {request?.requestStatus === 2 && (
                                                        <>
                                                            <IconButton
                                                                color="success"
                                                                onClick={() => {
                                                                    setCurrentRequestId(request.id);
                                                                    setAcceptDialogOpen(true);
                                                                }}
                                                            >
                                                                <CheckCircleIcon />
                                                            </IconButton>
                                                            <IconButton
                                                                color="error"
                                                                onClick={() => {
                                                                    setCurrentRequestId(request.id);
                                                                    setRejectDialogOpen(true);
                                                                }}
                                                            >
                                                                <CancelIcon />
                                                            </IconButton>
                                                        </>
                                                    )}
                                                </Stack>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>

                        </TableContainer>
                        <Stack direction="row" justifyContent="center" sx={{ mt: 3 }}>
                            <Pagination
                                count={totalPages}
                                page={pagination.pageNumber}
                                onChange={handlePageChange}
                                color="primary"
                            />
                        </Stack>
                    </>
                }
                {dialogOpen && <UpdateRequestDetail
                    open={dialogOpen}
                    onClose={handleCloseDialog}
                    request={selectedRequest}
                />}

                {acceptDialogOpen && (
                    <ConfirmAcceptDialog
                        open={acceptDialogOpen}
                        onClose={() => setAcceptDialogOpen(false)}
                        onConfirm={() => {
                            handleAccept(currentRequestId);
                            setAcceptDialogOpen(false);
                        }}
                    />
                )}

                {rejectDialogOpen && (
                    <ConfirmRejectDialog
                        open={rejectDialogOpen}
                        onClose={() => setRejectDialogOpen(false)}
                        onConfirm={(reason) => {
                            handleReject(currentRequestId, reason);
                            setRejectDialogOpen(false);
                        }}
                    />
                )}

                <LoadingComponent open={loading} setOpen={setLoading} />
            </Box>
        </Box >
    );
};

export default UpdateRequest;
