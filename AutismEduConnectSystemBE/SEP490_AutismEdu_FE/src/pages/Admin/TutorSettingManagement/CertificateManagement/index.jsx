import AddIcon from '@mui/icons-material/Add';
import axios from "~/plugins/axiosConfig";
import * as React from 'react';
import { Visibility as VisibilityIcon, Delete as DeleteIcon } from '@mui/icons-material';
import {
    Box,
    Button,
    FormControl,
    IconButton,
    InputLabel,
    MenuItem,
    Pagination,
    Paper,
    Select,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    TextField,
    Typography
} from '@mui/material';
import { useState } from 'react';
import { enqueueSnackbar } from 'notistack';
import services from '~/plugins/services';
import SearchIcon from '@mui/icons-material/Search';
import InputAdornment from '@mui/material/InputAdornment';
import { useNavigate } from 'react-router-dom';
import LoadingComponent from '~/components/LoadingComponent';
import CertificateDetailModal from './Modal/CertificateDetailModal';
import { format } from 'date-fns';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import ConfirmRejectDialog from './Modal/ConfirmRejectDialog';
import ConfirmAcceptDialog from './Modal/ConfirmAcceptDialog';
import emptyBook from '~/assets/images/icon/emptybook.gif'

const CertificateManagement = () => {
    const [currentSelectedId, setCurrentSelectedId] = useState(0);
    const [openDialogAccept, setOpenDialogAccept] = useState(false);
    const [openDialogReject, setOpenDialogReject] = useState(false);
    const [loading, setLoading] = useState(false);
    const [openDialog, setOpenDialog] = useState(false);
    const nav = useNavigate();
    const [filters, setFilters] = React.useState({
        search: '',
        status: 'all',
        orderBy: 'createdDate',
        sort: 'desc',
    });

    const [certificateList, setCertificateList] = useState([]);

    const [selectedCertificate, setSelectedCertificate] = useState(null);

    const [pagination, setPagination] = useState({
        pageNumber: 1,
        pageSize: 5,
        total: 10,
    });

    const [open, setOpen] = useState(false);

    const handleClickOpen = (id) => {
        setOpen(true);
    };

    const handleClose = () => {
        setOpen(false);
    };


    const handlePageChange = (event, value) => {
        setPagination({ ...pagination, pageNumber: value });
    };

    React.useEffect(() => {
        getCeritificates();
    }, [filters, pagination.pageNumber]);

    const getCeritificates = async () => {
        try {
            setLoading(true);
            await services.CertificateAPI.getCertificates((res) => {
                if (res?.result) {
                    setCertificateList(res.result);
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

    const handleViewDetail = (certificate) => {
        setSelectedCertificate(certificate);
        setOpenDialog(true);
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

    const statusText = (status) => {
        switch (status) {
            case 0:
                return 'Từ chối';
            case 1:
                return 'Chấp nhận';
            case 2:
                return 'Chờ duyệt';
            default:
                return 'Không rõ';
        }
    };
    const handleChangeStatus = async (id, statusChange, rejectionReason) => {
        try {
            setLoading(true);
            await services.CertificateAPI.changeStatusCertificate(id, {
                id,
                statusChange,
                rejectionReason
            }, (res) => {
                if (res?.result) {
                    let newData = certificateList.find((r) => r.id === id);
                    newData.requestStatus = res.result.requestStatus;
                    newData.rejectionReason = res.result.rejectionReason;
                    setCertificateList(certificateList);
                    enqueueSnackbar('Đổi trạng thái thành công!', { variant: 'success' });
                }
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: 'error' });
                console.log(error);
            });
        } catch (error) {
            console.log(error);

        } finally {
            setLoading(false);
        }
    };


    const handleAccept = async (id) => {
        await handleChangeStatus(id, 1, "");
        console.log(`Accepted request ID: ${id}`);
    };

    const handleReject = async (id, reason) => {
        await handleChangeStatus(id, 0, reason);
        console.log(`Rejected request ID: ${id} with reason: ${reason}`);
    };

    const totalPages = Math.ceil(pagination.total / pagination.pageSize);

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
                    height: certificateList?.length >= 5 ? 'auto' : '650px',
                    margin: "auto"
                }}
            >
                <Typography variant='h4' sx={{ mb: 3 }}>Danh sách chứng chỉ</Typography>
                <Stack direction={'row'} justifyContent={'space-between'} alignItems="center" sx={{ width: "100%", mb: 2 }} spacing={3}>
                    <Box sx={{ flex: 2, mr: 3 }}>
                        <TextField
                            fullWidth
                            size='small'
                            label="Tìm kiếm"
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
                        <Box sx={{ flexBasis: '55%' }}>
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

                {certificateList.length !== 0 ? <Box>
                    <TableContainer component={Paper} sx={{ mt: 3, boxShadow: 3, borderRadius: 2 }}>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell sx={{ fontWeight: 'bold' }}>STT</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Email</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Họ và tên</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Tên chứng chỉ</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Ngày tạo</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Trạng thái</TableCell>
                                    <TableCell sx={{ fontWeight: 'bold' }}>Hành động</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {certificateList.map((certificate, index) => (
                                    <TableRow key={certificate.id} hover>
                                        <TableCell>{index + 1 + (pagination?.pageNumber - 1) * 5}</TableCell>
                                        <TableCell>{certificate?.submitter?.email ?? certificate?.tutorRegistrationRequest?.email}</TableCell>
                                        <TableCell>{certificate?.submitter?.fullName ?? certificate?.tutorRegistrationRequest?.fullName}</TableCell>
                                        <TableCell
                                            sx={{
                                                maxWidth: 200,
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                whiteSpace: 'nowrap',
                                            }}
                                        >
                                            {certificate?.certificateName}
                                        </TableCell>
                                        <TableCell>{certificate?.createdDate && format(new Date(certificate.createdDate), "HH:mm dd/MM/yyyy")}</TableCell>
                                        <TableCell>
                                            <Button
                                                variant="outlined"
                                                color={
                                                    certificate.requestStatus === 1 ? 'success' :
                                                        certificate.requestStatus === 0 ? 'error' :
                                                            'warning'
                                                }
                                                size="small"
                                                sx={{ borderRadius: 2, textTransform: 'none' }}
                                            >
                                                {statusText(certificate?.requestStatus)}
                                            </Button>
                                        </TableCell>
                                        <TableCell>
                                            <IconButton color="primary" aria-label="xem chi tiết" onClick={() => handleViewDetail(certificate)}>
                                                <VisibilityIcon />
                                            </IconButton>
                                            {certificate?.requestStatus === 2 && <>
                                                <IconButton

                                                    color="success"
                                                    onClick={() => {
                                                        setCurrentSelectedId(certificate?.id);
                                                        setOpenDialogAccept(true);
                                                    }}
                                                >
                                                    <CheckCircleIcon />
                                                </IconButton>
                                                <IconButton

                                                    color="error"
                                                    onClick={() => {
                                                        setCurrentSelectedId(certificate?.id);
                                                        setOpenDialogReject(true);
                                                    }}
                                                >
                                                    <CancelIcon />
                                                </IconButton>
                                            </>}
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
                </Box> :
                    <Box sx={{ textAlign: "center" }}>
                        <img src={emptyBook} style={{ height: "200px" }} />
                        <Typography>Hiện tại không có chứng chỉ nào!</Typography>
                    </Box>}
                {openDialog && selectedCertificate && <CertificateDetailModal open={openDialog} onClose={() => setOpenDialog(false)} certificate={selectedCertificate} />}
                {openDialogReject && <ConfirmRejectDialog open={openDialogReject} onClose={() => setOpenDialogReject(false)} onConfirm={(reason) => {
                    handleReject(currentSelectedId, reason);
                    setOpenDialogReject(false);
                }} />}
                {openDialogAccept && <ConfirmAcceptDialog open={openDialogAccept} onClose={() => setOpenDialogAccept(false)} onConfirm={() => { handleAccept(currentSelectedId); setOpenDialogAccept(false); }} />}

                <LoadingComponent open={loading} setOpen={setLoading} />
            </Box>
        </Box>
    )
}

export default CertificateManagement